using LeilaoAuto.Application.Abstractions.Auth;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Infrastructure.Authentication;
using LeilaoAuto.Infrastructure.External;
using LeilaoAuto.Infrastructure.External.Connectors;
using LeilaoAuto.Infrastructure.Persistence;
using LeilaoAuto.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace LeilaoAuto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=leilaoauto;Username=postgres;Password=postgres";

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuctionProviderOptions>(configuration.GetSection(AuctionProviderOptions.SectionName));
        services.Configure<FipeOptions>(configuration.GetSection(FipeOptions.SectionName));
        services.AddOptions<BillingProviderOptions>()
            .Bind(configuration.GetSection(BillingProviderOptions.SectionName))
            .Validate(options =>
                options.Provider.Equals("Fake", StringComparison.OrdinalIgnoreCase)
                || options.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase),
                "Billing:Provider must be Fake or Stripe.")
            .ValidateOnStart();

        services.AddDbContext<LeilaoAutoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuctionLotRepository, AuctionLotRepository>();
        services.AddScoped<IUserEntityRepository, UserEntityRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IMonitoredVehicleRepository, MonitoredVehicleRepository>();
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<ILotAnalyticsRepository, LotAnalyticsRepository>();
        services.AddScoped<IConnectorExecutionLogRepository, ConnectorExecutionLogRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IBillingGateway, StubBillingGateway>();
        services.AddScoped<FakeBillingProvider>();
        services.AddScoped<StripeBillingProvider>();
        services.AddScoped<IBillingProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<BillingProviderOptions>>().Value;
            return options.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<StripeBillingProvider>()
                : provider.GetRequiredService<FakeBillingProvider>();
        });
        services.AddScoped<IAlertPublisher, StubAlertPublisher>();
        services.AddScoped<IAuctionProviderClient, AuctionProviderClient>();

        services.AddScoped<ILotConnector, SodreSantoroConnector>();
        services.AddScoped<ILotConnector, SuperbidConnector>();
        services.AddScoped<ILotConnector, VipLeiloesConnector>();
        services.AddScoped<ILotConnector, FreitasConnector>();
        services.AddScoped<ILotConnector, ZukermanConnector>();
        services.AddScoped<ILotConnector, MegaLeiloesConnector>();
        services.AddScoped<ILotConnector, PactoLeiloesConnector>();
        services.AddScoped<ILotConnector, MilanLeiloesConnector>();

        services.AddScoped<IConnectorRegistry, ConnectorRegistry>();
        services.AddScoped<IConnectorFactory, ConnectorFactory>();

        services.AddHttpClient<IFipePriceProvider, FipeHttpPriceProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<FipeOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient("lot-connectors", (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AuctionProviderOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(12);

                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Remove("X-Api-Key");
                    client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
                }
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeilaoAutoDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(dbContext, cancellationToken);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 4, durationOfBreak: TimeSpan.FromSeconds(20));
    }
}
