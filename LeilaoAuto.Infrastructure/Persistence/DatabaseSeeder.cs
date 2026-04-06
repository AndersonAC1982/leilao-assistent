using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var demoUser = new User("demo@leilaoauto.local", BCrypt.Net.BCrypt.HashPassword("Demo1234"));

            demoUser.AddMonitoredVehicle(new MonitoredVehicle(
                demoUser.Id,
                "Volkswagen",
                "Gol 1.6 MSI",
                2018,
                2022,
                VehicleType.Car,
                "SP",
                VehicleCondition.Unknown));

            demoUser.AddMonitoredVehicle(new MonitoredVehicle(
                demoUser.Id,
                "Honda",
                "CG 160 FAN",
                2020,
                2024,
                VehicleType.Motorcycle,
                "MG",
                VehicleCondition.Running));

            await dbContext.Users.AddAsync(demoUser, cancellationToken);
        }

        if (!await dbContext.AuctionLots.AnyAsync(cancellationToken))
        {
            var seedLots = new[]
            {
                new AuctionLot(
                    externalId: "seed-active-001",
                    auctioneer: "Leiloeiro Sul",
                    lotNumber: "3010",
                    make: "Volkswagen",
                    model: "Gol 1.6 MSI",
                    year: 2021,
                    vehicleType: VehicleType.Car,
                    uf: "SP",
                    vehicleCondition: VehicleCondition.Running,
                    status: LotStatus.Active,
                    lotUrl: "https://leiloeiro-sul.example/lote/3010",
                    currentBid: 29400m,
                    finalPrice: null,
                    appraisedValue: 36500m,
                    startsAt: DateTimeOffset.UtcNow.AddHours(-3),
                    endsAt: DateTimeOffset.UtcNow.AddHours(6)),

                new AuctionLot(
                    externalId: "seed-active-002",
                    auctioneer: "Leiloeiro Centro",
                    lotNumber: "1555",
                    make: "Toyota",
                    model: "Corolla GLI",
                    year: 2019,
                    vehicleType: VehicleType.Car,
                    uf: "RJ",
                    vehicleCondition: VehicleCondition.Damaged,
                    status: LotStatus.Active,
                    lotUrl: "https://leiloeiro-centro.example/lote/1555",
                    currentBid: 45100m,
                    finalPrice: null,
                    appraisedValue: 57800m,
                    startsAt: DateTimeOffset.UtcNow.AddHours(-2),
                    endsAt: DateTimeOffset.UtcNow.AddHours(2)),

                new AuctionLot(
                    externalId: "seed-closed-001",
                    auctioneer: "Leiloeiro Sul",
                    lotNumber: "2711",
                    make: "Volkswagen",
                    model: "Gol 1.6 MSI",
                    year: 2019,
                    vehicleType: VehicleType.Car,
                    uf: "SP",
                    vehicleCondition: VehicleCondition.Damaged,
                    status: LotStatus.Closed,
                    lotUrl: "https://leiloeiro-sul.example/lote/2711",
                    currentBid: null,
                    finalPrice: 32900m,
                    appraisedValue: 34900m,
                    startsAt: DateTimeOffset.UtcNow.AddDays(-14),
                    endsAt: DateTimeOffset.UtcNow.AddDays(-13)),

                new AuctionLot(
                    externalId: "seed-closed-002",
                    auctioneer: "Leiloeiro Sul",
                    lotNumber: "2712",
                    make: "Honda",
                    model: "CG 160 FAN",
                    year: 2022,
                    vehicleType: VehicleType.Motorcycle,
                    uf: "MG",
                    vehicleCondition: VehicleCondition.Running,
                    status: LotStatus.Closed,
                    lotUrl: "https://leiloeiro-sul.example/lote/2712",
                    currentBid: null,
                    finalPrice: 10200m,
                    appraisedValue: 11600m,
                    startsAt: DateTimeOffset.UtcNow.AddDays(-20),
                    endsAt: DateTimeOffset.UtcNow.AddDays(-19))
            };

            await dbContext.AuctionLots.AddRangeAsync(seedLots, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
