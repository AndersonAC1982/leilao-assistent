using LeilaoAuto.Application.Abstractions.Auth;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Infrastructure.Authentication;
using LeilaoAuto.Infrastructure.External;
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

        services.AddDbContext<LeilaoAutoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuctionLotRepository, AuctionLotRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IBillingGateway, StubBillingGateway>();
        services.AddScoped<IAlertPublisher, StubAlertPublisher>();

        services.AddHttpClient<IFipePriceProvider, FipeHttpPriceProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<FipeOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient<IAuctionProviderClient, AuctionProviderClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AuctionProviderOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(12);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeilaoAutoDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
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
