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
    public DbSet<MonitoredVehicle> MonitoredVehicles => Set<MonitoredVehicle>();
    public DbSet<AuctionLot> AuctionLots => Set<AuctionLot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeilaoAutoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
