using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly IReadOnlyDictionary<string, string> RealLotUrlsByAuctioneer =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Superbid"] = "https://www.superbid.net/oferta/veiculo-automotor-gm-omega-gls-4583144",
            ["VIP Leiloes"] = "https://www.vipleiloes.com.br/evento/anuncio/yamaha-ybr150-factor-25372",
            ["Sodre Santoro"] = "https://www.sodresantoro.com.br/veiculos/lotes?lot_brand=jeep&page=1",
            ["Mega Leiloes"] = "https://www.megaleiloes.com.br/imoveis/apartamentos/sp/sao-paulo/apartamento-218-m2-03-vagas-brooklin-paulista-sao-paulo-sp-x121107",
            ["Pacto Leiloes"] = "https://www.pactoleiloes.com.br/lotes/9590/2532/1/renault/clio/expression-16-hiflex-2007-2008-branca-dourados-ms",
            ["Freitas"] = "https://www.freitasleiloeiro.com.br/leiloes/lote?leilaoid=6055&lote=64",
            ["Milan Leiloes"] = "https://www.milanleiloes.com.br/Geral.asp?CL=13337",
            ["Zukerman"] = "https://www.portalzuk.com.br/leilao-de-imoveis/v/banco-bradesco/35860"
        };

    public static async Task SeedAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        await PatchPlaceholderLotUrlsAsync(dbContext, cancellationToken);

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

        var lots = new[]
        {
            new Lot(
                sourceSite: "Superbid",
                title: "Volkswagen Gol 1.6 MSI 2021",
                brand: "Volkswagen",
                model: "Gol 1.6 MSI",
                year: 2021,
                type: VehicleType.Car,
                uf: "MG",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Superbid"),
                imageUrl: null,
                description: "Lote ativo em andamento com documentacao regular.",
                currentPrice: 28600m,
                finalPrice: null,
                status: LotStatus.Active,
                foundAt: now.AddHours(-6),
                closedAt: null,
                rawDataJson: "{\"source\":\"seed\",\"status\":\"active\"}"),

            new Lot(
                sourceSite: "VIP Leiloes",
                title: "Toyota Corolla XEi 2020",
                brand: "Toyota",
                model: "Corolla XEi",
                year: 2020,
                type: VehicleType.Car,
                uf: "SP",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("VIP Leiloes"),
                imageUrl: null,
                description: "Lote confirmado com historico de revisoes.",
                currentPrice: 62500m,
                finalPrice: null,
                status: LotStatus.Confirmed,
                foundAt: now.AddHours(-4),
                closedAt: null,
                rawDataJson: "{\"source\":\"seed\",\"status\":\"confirmed\"}"),

            new Lot(
                sourceSite: "Sodre Santoro",
                title: "Jeep Renegade Longitude 2021",
                brand: "Jeep",
                model: "Renegade Longitude",
                year: 2021,
                type: VehicleType.Utility,
                uf: "SP",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Sodre Santoro"),
                imageUrl: null,
                description: "Lote ativo com baixa quilometragem declarada.",
                currentPrice: 78900m,
                finalPrice: null,
                status: LotStatus.Active,
                foundAt: now.AddHours(-2),
                closedAt: null,
                rawDataJson: "{\"source\":\"seed\",\"status\":\"active\"}"),

            new Lot(
                sourceSite: "Mega Leiloes",
                title: "Honda CG 160 Start 2022",
                brand: "Honda",
                model: "CG 160 Start",
                year: 2022,
                type: VehicleType.Motorcycle,
                uf: "MG",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Mega Leiloes"),
                imageUrl: null,
                description: "Lote encerrado com bom historico de lances.",
                currentPrice: null,
                finalPrice: 10180m,
                status: LotStatus.Closed,
                foundAt: now.AddDays(-8),
                closedAt: now.AddDays(-6),
                rawDataJson: "{\"source\":\"seed\",\"status\":\"closed\"}"),

            new Lot(
                sourceSite: "Pacto Leiloes",
                title: "Hyundai HB20 Comfort 2020",
                brand: "Hyundai",
                model: "HB20 Comfort",
                year: 2020,
                type: VehicleType.Car,
                uf: "RJ",
                vehicleState: VehicleCondition.Damaged,
                lotUrl: RealLotUrlFor("Pacto Leiloes"),
                imageUrl: null,
                description: "Lote encerrado com observacao de media monta.",
                currentPrice: null,
                finalPrice: 41200m,
                status: LotStatus.Closed,
                foundAt: now.AddDays(-10),
                closedAt: now.AddDays(-9),
                rawDataJson: "{\"source\":\"seed\",\"status\":\"closed\"}"),

            new Lot(
                sourceSite: "Freitas",
                title: "Chevrolet Onix LT 2021",
                brand: "Chevrolet",
                model: "Onix LT",
                year: 2021,
                type: VehicleType.Car,
                uf: "SP",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Freitas"),
                imageUrl: null,
                description: "Historico encerrado para composicao de media.",
                currentPrice: null,
                finalPrice: 53400m,
                status: LotStatus.Closed,
                foundAt: now.AddDays(-14),
                closedAt: now.AddDays(-13),
                rawDataJson: "{\"source\":\"seed\",\"status\":\"closed\"}"),

            new Lot(
                sourceSite: "Milan Leiloes",
                title: "Nissan Kicks SV 2020",
                brand: "Nissan",
                model: "Kicks SV",
                year: 2020,
                type: VehicleType.Utility,
                uf: "RJ",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Milan Leiloes"),
                imageUrl: null,
                description: "Lote ativo de referencia para monitoramento premium.",
                currentPrice: 65500m,
                finalPrice: null,
                status: LotStatus.Active,
                foundAt: now.AddHours(-3),
                closedAt: null,
                rawDataJson: "{\"source\":\"seed\",\"status\":\"active\"}"),

            new Lot(
                sourceSite: "Zukerman",
                title: "Toyota Hilux SRV 2019",
                brand: "Toyota",
                model: "Hilux SRV",
                year: 2019,
                type: VehicleType.Utility,
                uf: "MT",
                vehicleState: VehicleCondition.Running,
                lotUrl: RealLotUrlFor("Zukerman"),
                imageUrl: null,
                description: "Lote encerrado com finalizacao competitiva.",
                currentPrice: null,
                finalPrice: 121500m,
                status: LotStatus.Closed,
                foundAt: now.AddDays(-16),
                closedAt: now.AddDays(-15),
                rawDataJson: "{\"source\":\"seed\",\"status\":\"closed\"}")
        };

        var analytics = new[]
        {
            new LotAnalytics(
                normalizedModel: ModelNormalizer.NormalizeComparable("Gol 1.6 MSI", "Volkswagen"),
                averagePrice: 31950m,
                minPrice: 28400m,
                maxPrice: 35200m,
                sampleSize: 18,
                updatedAt: now),
            new LotAnalytics(
                normalizedModel: ModelNormalizer.NormalizeComparable("CG 160 Start", "Honda"),
                averagePrice: 10250m,
                minPrice: 9200m,
                maxPrice: 11100m,
                sampleSize: 15,
                updatedAt: now),
            new LotAnalytics(
                normalizedModel: ModelNormalizer.NormalizeComparable("Corolla XEi", "Toyota"),
                averagePrice: 64300m,
                minPrice: 58900m,
                maxPrice: 70100m,
                sampleSize: 12,
                updatedAt: now),
            new LotAnalytics(
                normalizedModel: ModelNormalizer.NormalizeComparable("Onix LT", "Chevrolet"),
                averagePrice: 54220m,
                minPrice: 49800m,
                maxPrice: 59700m,
                sampleSize: 21,
                updatedAt: now),
            new LotAnalytics(
                normalizedModel: ModelNormalizer.NormalizeComparable("HB20 Comfort", "Hyundai"),
                averagePrice: 43810m,
                minPrice: 40100m,
                maxPrice: 47200m,
                sampleSize: 14,
                updatedAt: now)
        };

        var connectorLogs = new[]
        {
            new ConnectorExecutionLog(
                connectorName: "Superbid",
                executedAt: now.AddMinutes(-55),
                success: true,
                recordsRead: 24,
                recordsSaved: 14,
                message: "Seeded connector execution log for baseline telemetry.",
                payloadJson: "{\"phase\":\"10\",\"connector\":\"superbid\",\"discarded\":10}"),
            new ConnectorExecutionLog(
                connectorName: "VipLeiloes",
                executedAt: now.AddMinutes(-40),
                success: true,
                recordsRead: 17,
                recordsSaved: 11,
                message: "Seeded connector execution log for baseline telemetry.",
                payloadJson: "{\"phase\":\"10\",\"connector\":\"vip\",\"discarded\":6}"),
            new ConnectorExecutionLog(
                connectorName: "MilanLeiloes",
                executedAt: now.AddMinutes(-25),
                success: false,
                recordsRead: 0,
                recordsSaved: 0,
                message: "Timeout while reaching upstream endpoint.",
                payloadJson: "{\"phase\":\"10\",\"connector\":\"milan\",\"error\":\"timeout\"}")
        };

        await dbContext.Users.AddRangeAsync([admin, proUser, premiumUser, freeUser], cancellationToken);
        await dbContext.Subscriptions.AddRangeAsync(subscriptions, cancellationToken);
        await dbContext.Lots.AddRangeAsync(lots, cancellationToken);
        await dbContext.LotAnalytics.AddRangeAsync(analytics, cancellationToken);
        await dbContext.ConnectorExecutionLogs.AddRangeAsync(connectorLogs, cancellationToken);

        await SeedLegacyAuctionLotsAsync(dbContext, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string RealLotUrlFor(string sourceOrAuctioneer)
    {
        return RealLotUrlsByAuctioneer.TryGetValue(sourceOrAuctioneer, out var url)
            ? url
            : "https://www.superbid.net/oferta/veiculo-automotor-gm-omega-gls-4583144";
    }

    private static async Task PatchPlaceholderLotUrlsAsync(LeilaoAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var mapping in RealLotUrlsByAuctioneer)
        {
            await dbContext.Lots
                .Where(lot => lot.SourceSite == mapping.Key && EF.Functions.Like(lot.LotUrl, "%.example%"))
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(lot => lot.LotUrl, mapping.Value),
                    cancellationToken);

            await dbContext.AuctionLots
                .Where(lot => lot.Auctioneer == mapping.Key && EF.Functions.Like(lot.LotUrl, "%.example%"))
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(lot => lot.LotUrl, mapping.Value),
                    cancellationToken);
        }
    }

    private static void AttachVehicles(User user, params MonitoredVehicle[] vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            user.AddMonitoredVehicle(vehicle);
        }
    }

    private static async Task SeedLegacyAuctionLotsAsync(LeilaoAutoDbContext dbContext, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (await dbContext.AuctionLots.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = new DateTimeOffset(nowUtc, TimeSpan.Zero);
        var legacyLots = new[]
        {
            new AuctionLot(
                externalId: "legacy-superbid-active-gol-001",
                auctioneer: "Superbid",
                lotNumber: "1001",
                make: "Volkswagen",
                model: "Gol 1.6 MSI",
                year: 2021,
                vehicleType: VehicleType.Car,
                uf: "MG",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Active,
                lotUrl: RealLotUrlFor("Superbid"),
                currentBid: 28700m,
                finalPrice: null,
                appraisedValue: 34200m,
                startsAt: now.AddHours(-7),
                endsAt: now.AddHours(4)),
            new AuctionLot(
                externalId: "legacy-vip-confirmed-corolla-001",
                auctioneer: "VIP Leiloes",
                lotNumber: "1101",
                make: "Toyota",
                model: "Corolla XEi",
                year: 2020,
                vehicleType: VehicleType.Car,
                uf: "SP",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Confirmed,
                lotUrl: RealLotUrlFor("VIP Leiloes"),
                currentBid: 62100m,
                finalPrice: null,
                appraisedValue: 69500m,
                startsAt: now.AddHours(-5),
                endsAt: now.AddHours(3)),
            new AuctionLot(
                externalId: "legacy-sodre-active-renegade-001",
                auctioneer: "Sodre Santoro",
                lotNumber: "1201",
                make: "Jeep",
                model: "Renegade Longitude",
                year: 2021,
                vehicleType: VehicleType.Utility,
                uf: "SP",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Active,
                lotUrl: RealLotUrlFor("Sodre Santoro"),
                currentBid: 79200m,
                finalPrice: null,
                appraisedValue: 88400m,
                startsAt: now.AddHours(-2),
                endsAt: now.AddHours(5)),
            new AuctionLot(
                externalId: "legacy-freitas-active-kicks-001",
                auctioneer: "Freitas",
                lotNumber: "1301",
                make: "Nissan",
                model: "Kicks SV",
                year: 2020,
                vehicleType: VehicleType.Utility,
                uf: "RJ",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Active,
                lotUrl: RealLotUrlFor("Freitas"),
                currentBid: 65400m,
                finalPrice: null,
                appraisedValue: 73200m,
                startsAt: now.AddHours(-3),
                endsAt: now.AddHours(6)),
            new AuctionLot(
                externalId: "legacy-mega-closed-cg160-001",
                auctioneer: "Mega Leiloes",
                lotNumber: "2101",
                make: "Honda",
                model: "CG 160 Start",
                year: 2022,
                vehicleType: VehicleType.Motorcycle,
                uf: "MG",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Closed,
                lotUrl: RealLotUrlFor("Mega Leiloes"),
                currentBid: null,
                finalPrice: 10150m,
                appraisedValue: 11300m,
                startsAt: now.AddDays(-6),
                endsAt: now.AddDays(-5)),
            new AuctionLot(
                externalId: "legacy-zukerman-closed-hilux-001",
                auctioneer: "Zukerman",
                lotNumber: "2201",
                make: "Toyota",
                model: "Hilux SRV",
                year: 2019,
                vehicleType: VehicleType.Utility,
                uf: "MT",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Closed,
                lotUrl: RealLotUrlFor("Zukerman"),
                currentBid: null,
                finalPrice: 121900m,
                appraisedValue: 136000m,
                startsAt: now.AddDays(-8),
                endsAt: now.AddDays(-7)),
            new AuctionLot(
                externalId: "legacy-pacto-closed-hb20-001",
                auctioneer: "Pacto Leiloes",
                lotNumber: "2301",
                make: "Hyundai",
                model: "HB20 Comfort",
                year: 2020,
                vehicleType: VehicleType.Car,
                uf: "RJ",
                vehicleCondition: VehicleCondition.Damaged,
                status: LotStatus.Closed,
                lotUrl: RealLotUrlFor("Pacto Leiloes"),
                currentBid: null,
                finalPrice: 41300m,
                appraisedValue: 47900m,
                startsAt: now.AddDays(-10),
                endsAt: now.AddDays(-9)),
            new AuctionLot(
                externalId: "legacy-milan-closed-onix-001",
                auctioneer: "Milan Leiloes",
                lotNumber: "2401",
                make: "Chevrolet",
                model: "Onix LT",
                year: 2021,
                vehicleType: VehicleType.Car,
                uf: "SP",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Closed,
                lotUrl: RealLotUrlFor("Milan Leiloes"),
                currentBid: null,
                finalPrice: 53350m,
                appraisedValue: 59600m,
                startsAt: now.AddDays(-11),
                endsAt: now.AddDays(-10)),
            new AuctionLot(
                externalId: "legacy-superbid-closed-gol-001",
                auctioneer: "Superbid",
                lotNumber: "2501",
                make: "Volkswagen",
                model: "Gol 1.6 MSI",
                year: 2021,
                vehicleType: VehicleType.Car,
                uf: "MG",
                vehicleCondition: VehicleCondition.Running,
                status: LotStatus.Closed,
                lotUrl: RealLotUrlFor("Superbid"),
                currentBid: null,
                finalPrice: 31600m,
                appraisedValue: 35100m,
                startsAt: now.AddDays(-12),
                endsAt: now.AddDays(-11))
        };

        await dbContext.AuctionLots.AddRangeAsync(legacyLots, cancellationToken);
    }
}
