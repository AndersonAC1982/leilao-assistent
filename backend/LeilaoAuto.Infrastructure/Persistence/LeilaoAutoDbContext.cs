using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence;

public class LeilaoAutoDbContext : DbContext
{
    public LeilaoAutoDbContext(DbContextOptions<LeilaoAutoDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MonitoredVehicle> MonitoredVehicles => Set<MonitoredVehicle>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<LotAnalytics> LotAnalytics => Set<LotAnalytics>();
    public DbSet<ConnectorExecutionLog> ConnectorExecutionLogs => Set<ConnectorExecutionLog>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    // Backward-compatibility with phase-1 modules.
    public DbSet<AuctionLot> AuctionLots => Set<AuctionLot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeilaoAutoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
