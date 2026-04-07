using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LeilaoAuto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IModelNormalizationService, ModelNormalizationService>();
        services.AddScoped<ILotAnalyticsComputationService, LotAnalyticsComputationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<ILotService, LotService>();
        return services;
    }
}
