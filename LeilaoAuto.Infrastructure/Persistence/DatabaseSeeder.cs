using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var admin = new User(
            email: "admin@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin1234"),
            role: UserRole.Admin,
            plan: PlanType.Enterprise);

        var standardUser = new User(
            email: "demo@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Demo1234"),
            role: UserRole.User,
            plan: PlanType.Pro);

        var adminVehicles = new[]
        {
            new MonitoredVehicle(admin.Id, "Toyota", "Corolla XEi", 2020, VehicleType.Car, "SP", VehicleCondition.Running),
            new MonitoredVehicle(admin.Id, "Honda", "Civic EX", 2019, VehicleType.Car, "SP", VehicleCondition.Unknown)
        };

        var userVehicles = new[]
        {
            new MonitoredVehicle(standardUser.Id, "Volkswagen", "Gol 1.6 MSI", 2021, VehicleType.Car, "MG", VehicleCondition.Running),
            new MonitoredVehicle(standardUser.Id, "Honda", "CG 160 FAN", 2022, VehicleType.Motorcycle, "MG", VehicleCondition.Running)
        };

        foreach (var vehicle in adminVehicles)
        {
            admin.AddMonitoredVehicle(vehicle);
        }

        foreach (var vehicle in userVehicles)
        {
            standardUser.AddMonitoredVehicle(vehicle);
        }

        var subscriptions = new[]
        {
            new Subscription(
                admin.Id,
                provider: "ManualSeed",
                externalCustomerId: "admin-customer-001",
                externalSubscriptionId: "admin-sub-001",
                status: SubscriptionStatus.Active,
                plan: PlanType.Enterprise,
                startedAt: DateTime.UtcNow.AddMonths(-3),
                endsAt: DateTime.UtcNow.AddMonths(9)),
            new Subscription(
                standardUser.Id,
                provider: "ManualSeed",
                externalCustomerId: "user-customer-001",
                externalSubscriptionId: "user-sub-001",
                status: SubscriptionStatus.Active,
                plan: PlanType.Pro,
                startedAt: DateTime.UtcNow.AddMonths(-1),
                endsAt: DateTime.UtcNow.AddMonths(11))
        };

        var lots = new[]
        {
            new Lot(
                sourceSite: "Leiloeiro Sul",
                title: "Volkswagen Gol 1.6 MSI 2021",
                brand: "Volkswagen",
                model: "Gol 1.6 MSI",
                year: 2021,
                type: VehicleType.Car,
                uf: "MG",
                vehicleState: VehicleCondition.Running,
                lotUrl: "https://leiloeiro-sul.example/lote/1001",
                imageUrl: "https://leiloeiro-sul.example/lote/1001/image.jpg",
                description: "Lote ativo de exemplo",
                currentPrice: 28900m,
                finalPrice: null,
                status: LotStatus.Active,
                foundAt: DateTime.UtcNow.AddHours(-5),
                closedAt: null,
                rawDataJson: "{\"seed\":true,\"status\":\"active\"}"),

            new Lot(
                sourceSite: "Leiloeiro Centro",
                title: "Honda CG 160 FAN 2022",
                brand: "Honda",
                model: "CG 160 FAN",
                year: 2022,
                type: VehicleType.Motorcycle,
                uf: "MG",
                vehicleState: VehicleCondition.Running,
                lotUrl: "https://leiloeiro-centro.example/lote/2001",
                imageUrl: null,
                description: "Lote encerrado de exemplo",
                currentPrice: null,
                finalPrice: 10350m,
                status: LotStatus.Closed,
                foundAt: DateTime.UtcNow.AddDays(-10),
                closedAt: DateTime.UtcNow.AddDays(-7),
                rawDataJson: "{\"seed\":true,\"status\":\"closed\"}"),

            new Lot(
                sourceSite: "Leiloeiro Capital",
                title: "Toyota Corolla XEi 2020",
                brand: "Toyota",
                model: "Corolla XEi",
                year: 2020,
                type: VehicleType.Car,
                uf: "SP",
                vehicleState: VehicleCondition.Damaged,
                lotUrl: "https://leiloeiro-capital.example/lote/3002",
                imageUrl: null,
                description: "Lote confirmado com URL valida",
                currentPrice: 51200m,
                finalPrice: null,
                status: LotStatus.Confirmed,
                foundAt: DateTime.UtcNow.AddHours(-8),
                closedAt: null,
                rawDataJson: "{\"seed\":true,\"status\":\"confirmed\"}")
        };

        var analytics = new[]
        {
            new LotAnalytics(
                normalizedModel: "GOL 1 6 MSI",
                averagePrice: 32150m,
                minPrice: 28900m,
                maxPrice: 35200m,
                sampleSize: 12,
                updatedAt: DateTime.UtcNow),

            new LotAnalytics(
                normalizedModel: "CG 160 FAN",
                averagePrice: 10100m,
                minPrice: 9200m,
                maxPrice: 10900m,
                sampleSize: 9,
                updatedAt: DateTime.UtcNow)
        };

        var connectorLogs = new[]
        {
            new ConnectorExecutionLog(
                connectorName: "seed-lot-import",
                executedAt: DateTime.UtcNow.AddMinutes(-45),
                success: true,
                recordsRead: 3,
                recordsSaved: 3,
                message: "Seed initialization import.",
                payloadJson: "{\"phase\":\"2\",\"operation\":\"seed\"}")
        };

        await dbContext.Users.AddRangeAsync([admin, standardUser], cancellationToken);
        await dbContext.Subscriptions.AddRangeAsync(subscriptions, cancellationToken);
        await dbContext.Lots.AddRangeAsync(lots, cancellationToken);
        await dbContext.LotAnalytics.AddRangeAsync(analytics, cancellationToken);
        await dbContext.ConnectorExecutionLogs.AddRangeAsync(connectorLogs, cancellationToken);

        // Compatibility seed for phase-1 endpoints still using AuctionLot.
        await SeedLegacyAuctionLotsAsync(dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedLegacyAuctionLotsAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.AuctionLots.AnyAsync(cancellationToken))
        {
            return;
        }

        var legacyLots = new[]
        {
            new AuctionLot(
                externalId: "legacy-seed-active-001",
                auctioneer: "Leiloeiro Sul",
                lotNumber: "1001",
                make: "Volkswagen",
                model: "Gol 1.6 MSI",
                year: 2021,
                vehicleType: VehicleType.Car,
                uf: "MG",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Active,
                lotUrl: "https://leiloeiro-sul.example/lote/1001",
                currentBid: 28900m,
                finalPrice: null,
                appraisedValue: 34000m,
                startsAt: DateTimeOffset.UtcNow.AddHours(-6),
                endsAt: DateTimeOffset.UtcNow.AddHours(2)),
            new AuctionLot(
                externalId: "legacy-seed-closed-001",
                auctioneer: "Leiloeiro Centro",
                lotNumber: "2001",
                make: "Honda",
                model: "CG 160 FAN",
                year: 2022,
                vehicleType: VehicleType.Motorcycle,
                uf: "MG",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Closed,
                lotUrl: "https://leiloeiro-centro.example/lote/2001",
                currentBid: null,
                finalPrice: 10350m,
                appraisedValue: 11800m,
                startsAt: DateTimeOffset.UtcNow.AddDays(-9),
                endsAt: DateTimeOffset.UtcNow.AddDays(-7))
        };

        await dbContext.AuctionLots.AddRangeAsync(legacyLots, cancellationToken);
    }
}


