using LeilaoAuto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LeilaoAuto.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly LeilaoAutoDbContext _dbContext;

    public DatabaseHealthCheck(LeilaoAutoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", exception);
        }
    }
}
