using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        await PurgeSyntheticSeedDataAsync(dbContext, cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var admin = new User(
            email: "admin@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin1234"),
            role: UserRole.Admin,
            plan: PlanType.Elite);

        var proUser = new User(
            email: "pro@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Pro1234"),
            role: UserRole.User,
            plan: PlanType.Pro);

        var premiumUser = new User(
            email: "premium@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Premium1234"),
            role: UserRole.User,
            plan: PlanType.Premium);

        var freeUser = new User(
            email: "free@leilaoauto.local",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Free1234"),
            role: UserRole.User,
            plan: PlanType.Free);

        AttachVehicles(
            admin,
            new MonitoredVehicle(admin.Id, "Toyota", "Corolla XEi", 2020, VehicleType.Car, "SP", VehicleCondition.Running),
            new MonitoredVehicle(admin.Id, "Chevrolet", "Onix LT", 2021, VehicleType.Car, "SP", VehicleCondition.Running),
            new MonitoredVehicle(admin.Id, "Honda", "CG 160 Start", 2022, VehicleType.Motorcycle, "MG", VehicleCondition.Running),
            new MonitoredVehicle(admin.Id, "Hyundai", "HB20 Comfort", 2020, VehicleType.Car, "RJ", VehicleCondition.Damaged));

        AttachVehicles(
            proUser,
            new MonitoredVehicle(proUser.Id, "Volkswagen", "Gol 1.6 MSI", 2021, VehicleType.Car, "MG", VehicleCondition.Running),
            new MonitoredVehicle(proUser.Id, "Honda", "Civic EXL", 2019, VehicleType.Car, "SP", VehicleCondition.Running),
            new MonitoredVehicle(proUser.Id, "Fiat", "Toro Freedom", 2022, VehicleType.Utility, "PR", VehicleCondition.Running));

        AttachVehicles(
            premiumUser,
            new MonitoredVehicle(premiumUser.Id, "Jeep", "Renegade Longitude", 2021, VehicleType.Utility, "SP", VehicleCondition.Running),
            new MonitoredVehicle(premiumUser.Id, "Nissan", "Kicks SV", 2020, VehicleType.Utility, "RJ", VehicleCondition.Running),
            new MonitoredVehicle(premiumUser.Id, "Yamaha", "Fazer 250", 2022, VehicleType.Motorcycle, "MG", VehicleCondition.Running),
            new MonitoredVehicle(premiumUser.Id, "Toyota", "Hilux SRV", 2019, VehicleType.Utility, "MT", VehicleCondition.Running));

        AttachVehicles(
            freeUser,
            new MonitoredVehicle(freeUser.Id, "Renault", "Kwid Zen", 2021, VehicleType.Car, "BA", VehicleCondition.Running),
            new MonitoredVehicle(freeUser.Id, "Chevrolet", "S10 LT", 2018, VehicleType.Utility, "GO", VehicleCondition.TheftRecovery));

        var subscriptions = new[]
        {
            new Subscription(
                admin.Id,
                provider: "ManualSeed",
                externalCustomerId: "seed-customer-admin",
                externalSubscriptionId: "seed-sub-admin-elite",
                status: SubscriptionStatus.Active,
                plan: PlanType.Elite,
                startedAt: now.AddMonths(-6),
                endsAt: now.AddMonths(6)),
            new Subscription(
                proUser.Id,
                provider: "ManualSeed",
                externalCustomerId: "seed-customer-pro",
                externalSubscriptionId: "seed-sub-pro",
                status: SubscriptionStatus.Active,
                plan: PlanType.Pro,
                startedAt: now.AddMonths(-2),
                endsAt: now.AddMonths(10)),
            new Subscription(
                premiumUser.Id,
                provider: "ManualSeed",
                externalCustomerId: "seed-customer-premium",
                externalSubscriptionId: "seed-sub-premium",
                status: SubscriptionStatus.Active,
                plan: PlanType.Premium,
                startedAt: now.AddMonths(-1),
                endsAt: now.AddMonths(11)),
            new Subscription(
                freeUser.Id,
                provider: "ManualSeed",
                externalCustomerId: "seed-customer-free",
                externalSubscriptionId: "seed-sub-free-expired",
                status: SubscriptionStatus.Expired,
                plan: PlanType.Free,
                startedAt: now.AddMonths(-13),
                endsAt: now.AddMonths(-1))
        };

        var userSettings = new[]
        {
            new UserSettings(
                admin.Id,
                search: string.Empty,
                source: string.Empty,
                minScore: 70m,
                vehicleType: null,
                region: "SP",
                advancedFiltersEnabled: true,
                updatedAt: now,
                category: "Todas",
                activeSources: "Superbid|Sodre Santoro|VIP Leiloes|Freitas|Zukerman|Mega Leiloes|Pacto Leiloes|Milan Leiloes",
                maxPrice: null),
            new UserSettings(
                proUser.Id,
                search: "gol",
                source: "Superbid",
                minScore: 65m,
                vehicleType: (int)VehicleType.Car,
                region: "MG",
                advancedFiltersEnabled: true,
                updatedAt: now,
                category: "Veiculos",
                activeSources: "Superbid|Freitas",
                maxPrice: 85000m),
            new UserSettings(
                premiumUser.Id,
                search: "renegade",
                source: string.Empty,
                minScore: 60m,
                vehicleType: (int)VehicleType.Utility,
                region: "SP",
                advancedFiltersEnabled: true,
                updatedAt: now,
                category: "Veiculos",
                activeSources: "Sodre Santoro|VIP Leiloes|Milan Leiloes",
                maxPrice: 150000m),
            new UserSettings(
                freeUser.Id,
                search: string.Empty,
                source: string.Empty,
                minScore: 60m,
                vehicleType: null,
                region: null,
                advancedFiltersEnabled: false,
                updatedAt: now,
                category: "Todas",
                activeSources: "Superbid|Pacto Leiloes",
                maxPrice: null)
        };

        await dbContext.Users.AddRangeAsync([admin, proUser, premiumUser, freeUser], cancellationToken);
        await dbContext.Subscriptions.AddRangeAsync(subscriptions, cancellationToken);
        await dbContext.UserSettings.AddRangeAsync(userSettings, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task PurgeSyntheticSeedDataAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.AuctionLots
            .Where(lot =>
                lot.ExternalId.StartsWith("legacy-")
                || lot.ExternalId.EndsWith("-active-001")
                || lot.ExternalId.EndsWith("-closed-001")
                || EF.Functions.Like(lot.LotUrl, "%.example%")
                || EF.Functions.Like(lot.LotUrl, "%.invalid%")
                || EF.Functions.Like(lot.LotUrl, "%.test%"))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Lots
            .Where(lot =>
                (lot.RawDataJson != null && lot.RawDataJson.Contains("\"source\":\"seed\""))
                || EF.Functions.Like(lot.LotUrl, "%.example%")
                || EF.Functions.Like(lot.LotUrl, "%.invalid%")
                || EF.Functions.Like(lot.LotUrl, "%.test%"))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ConnectorExecutionLogs
            .Where(log =>
                (log.Message != null && log.Message.Contains("Seeded connector execution log"))
                || (log.PayloadJson != null && log.PayloadJson.Contains("\"phase\":\"10\"")))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static void AttachVehicles(User user, params MonitoredVehicle[] vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            user.AddMonitoredVehicle(vehicle);
        }
    }

}
