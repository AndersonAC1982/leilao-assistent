using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LeilaoAuto.Infrastructure.Persistence;

public class LeilaoAutoDbContextFactory : IDesignTimeDbContextFactory<LeilaoAutoDbContext>
{
    public LeilaoAutoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LeilaoAutoDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=leilaoauto;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new LeilaoAutoDbContext(optionsBuilder.Options);
    }
}
