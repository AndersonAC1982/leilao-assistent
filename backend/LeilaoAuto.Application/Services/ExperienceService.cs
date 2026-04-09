using System.Text.Json;
using System.Diagnostics;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LeilaoAuto.Application.Services;

public sealed class ExperienceService : IExperienceService
{
    private const string ManualScannerConnectorName = "ScannerManual";

    private readonly ILotService _lotService;
    private readonly IConnectorFactory _connectorFactory;
    private readonly IUserRepository _userRepository;
    private readonly IConnectorExecutionLogRepository _connectorExecutionLogRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ILogger<ExperienceService> _logger;

    public ExperienceService(
        ILotService lotService,
        IConnectorFactory connectorFactory,
        IUserRepository userRepository,
        IConnectorExecutionLogRepository connectorExecutionLogRepository,
        IUserSettingsRepository userSettingsRepository,
        ILogger<ExperienceService> logger)
    {
        _lotService = lotService;
        _connectorFactory = connectorFactory;
        _userRepository = userRepository;
        _connectorExecutionLogRepository = connectorExecutionLogRepository;
        _userSettingsRepository = userSettingsRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OpportunityFeedItemDto>> GetOpportunitiesAsync(
        Guid userId,
        OpportunityFeedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var quota = GetQuota(user.Plan);

        var filter = new LotSearchFilterRequest
        {
            Model = request.Model,
            Year = request.Year,
            VehicleType = request.VehicleType.HasValue ? (VehicleType?)request.VehicleType.Value : null,
            Uf = request.Uf ?? request.Region,
            VehicleCondition = request.VehicleCondition.HasValue ? (VehicleCondition?)request.VehicleCondition.Value : null
        };

        var searchResult = await _lotService.SearchAsync(userId, filter, cancellationToken);
        if (searchResult.ActiveLots.Count == 0)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Opportunities request finished with no active lots. UserId={UserId}, Plan={Plan}, ElapsedMs={ElapsedMs}.",
                userId,
                user.Plan,
                stopwatch.ElapsedMilliseconds);
            return [];
        }

        var invalidUrlDiscarded = 0;

        var opportunities = searchResult.ActiveLots
            .Where(lot =>
            {
                var valid = IsExactLotUrlForConnector(lot.LotUrl);
                if (!valid)
                {
                    invalidUrlDiscarded++;
                }

                return valid;
            })
            .Where(lot => quota.AllowsAdvancedConnectors || !IsAdvancedConnector(lot.Source, lot.Auctioneer))
            .Where(lot => string.IsNullOrWhiteSpace(request.Source)
                || lot.Source.Contains(request.Source, StringComparison.OrdinalIgnoreCase)
                || lot.Auctioneer.Contains(request.Source, StringComparison.OrdinalIgnoreCase))
            .Where(lot => !request.MinScore.HasValue || lot.OpportunityScore >= request.MinScore.Value)
            .Where(lot => string.IsNullOrWhiteSpace(request.Region)
                || lot.Uf.Equals(request.Region, StringComparison.OrdinalIgnoreCase))
            .Where(lot => string.IsNullOrWhiteSpace(request.Search)
                || ContainsSearch(lot, request.Search))
            .OrderByDescending(lot => lot.OpportunityScore)
            .ThenBy(lot => lot.RiskScore)
            .Select(MapOpportunity)
            .Take(quota.MaxOpportunityResults)
            .ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "Opportunities served. UserId={UserId}, Plan={Plan}, Returned={Returned}, MaxAllowed={MaxAllowed}, InvalidUrlDiscarded={InvalidUrlDiscarded}, ElapsedMs={ElapsedMs}.",
            userId,
            user.Plan,
            opportunities.Count,
            quota.MaxOpportunityResults,
            invalidUrlDiscarded,
            stopwatch.ElapsedMilliseconds);

        return opportunities;
    }

    public async Task<ScannerRunResponseDto> RunScannerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var quota = GetQuota(user.Plan);
        var utcNow = DateTime.UtcNow;
        var dayStartUtc = utcNow.Date;
        var dayEndUtc = dayStartUtc.AddDays(1);
        var scannerRunsToday = await _connectorExecutionLogRepository.CountByUserAndConnectorAsync(
            userId,
            ManualScannerConnectorName,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);

        if (scannerRunsToday >= quota.MaxScannerRunsPerDay)
        {
            _logger.LogWarning(
                "Scanner quota exceeded. UserId={UserId}, Plan={Plan}, DailyLimit={DailyLimit}, RunsToday={RunsToday}.",
                userId,
                user.Plan,
                quota.MaxScannerRunsPerDay,
                scannerRunsToday);

            throw new DomainRuleException(
                $"Limite diário de varreduras do plano {user.Plan.ToDisplayName()} atingido ({quota.MaxScannerRunsPerDay}/dia).");
        }

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var refreshed = await _lotService.RefreshAsync(cancellationToken);
            var completedAt = DateTimeOffset.UtcNow;
            stopwatch.Stop();

            var payload = JsonSerializer.Serialize(new
            {
                userId,
                plan = user.Plan.ToDisplayName(),
                refreshed,
                startedAt,
                completedAt,
                source = "api/scanner/run",
                scannerRunsToday = scannerRunsToday + 1,
                scannerDailyLimit = quota.MaxScannerRunsPerDay,
                elapsedMs = stopwatch.ElapsedMilliseconds
            });

            await _connectorExecutionLogRepository.AddAsync(
                new ConnectorExecutionLog(
                    connectorName: ManualScannerConnectorName,
                    executedAt: completedAt.UtcDateTime,
                    success: true,
                    recordsRead: refreshed,
                    recordsSaved: refreshed,
                    message: "Manual scanner run from extension/web/mobile facade.",
                    payloadJson: payload,
                    userId: userId),
                cancellationToken);

            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Scanner run succeeded. UserId={UserId}, Plan={Plan}, RefreshedLots={RefreshedLots}, ElapsedMs={ElapsedMs}.",
                userId,
                user.Plan,
                refreshed,
                stopwatch.ElapsedMilliseconds);

            return new ScannerRunResponseDto(
                startedAt,
                completedAt,
                refreshed,
                true,
                $"Varredura concluida com sucesso. {refreshed} lotes atualizados.");
        }
        catch (DomainRuleException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var completedAt = DateTimeOffset.UtcNow;
            stopwatch.Stop();
            var payload = JsonSerializer.Serialize(new
            {
                userId,
                plan = user.Plan.ToDisplayName(),
                startedAt,
                completedAt,
                source = "api/scanner/run",
                error = exception.Message,
                elapsedMs = stopwatch.ElapsedMilliseconds
            });

            await _connectorExecutionLogRepository.AddAsync(
                new ConnectorExecutionLog(
                    connectorName: ManualScannerConnectorName,
                    executedAt: completedAt.UtcDateTime,
                    success: false,
                    recordsRead: 0,
                    recordsSaved: 0,
                    message: exception.Message,
                    payloadJson: payload,
                    userId: userId),
                cancellationToken);

            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                exception,
                "Scanner run failed. UserId={UserId}, Plan={Plan}, ElapsedMs={ElapsedMs}.",
                userId,
                user.Plan,
                stopwatch.ElapsedMilliseconds);

            return new ScannerRunResponseDto(
                startedAt,
                completedAt,
                0,
                false,
                "A varredura falhou. Verifique os conectores e tente novamente.");
        }
    }

    public async Task<IReadOnlyList<HistoryItemDto>> GetHistoryAsync(Guid userId, int take, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var quota = GetQuota(user.Plan);
        var safeTake = Math.Clamp(take, 1, quota.MaxHistoryItems);
        var logs = await _connectorExecutionLogRepository.GetRecentByUserIdAsync(userId, safeTake, cancellationToken);

        var response = logs
            .Select(log => new HistoryItemDto(
                log.Id,
                log.ConnectorName,
                new DateTimeOffset(DateTime.SpecifyKind(log.ExecutedAt, DateTimeKind.Utc)),
                log.Success,
                log.RecordsRead,
                log.RecordsSaved,
                log.RecordsSaved,
                log.Success ? "CONCLUIDO" : "FALHA",
                log.Message))
            .ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "History served. UserId={UserId}, Plan={Plan}, RequestedTake={RequestedTake}, EffectiveTake={EffectiveTake}, Returned={Returned}, ElapsedMs={ElapsedMs}.",
            userId,
            user.Plan,
            take,
            safeTake,
            response.Count,
            stopwatch.ElapsedMilliseconds);

        return response;
    }

    public async Task<UserSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var quota = GetQuota(user.Plan);

        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        if (!quota.AllowsAdvancedFilters && settings.AdvancedFiltersEnabled)
        {
            settings = settings with { AdvancedFiltersEnabled = false };
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Settings fetched. UserId={UserId}, Plan={Plan}, AdvancedFiltersAllowed={AdvancedFiltersAllowed}, ElapsedMs={ElapsedMs}.",
            userId,
            user.Plan,
            quota.AllowsAdvancedFilters,
            stopwatch.ElapsedMilliseconds);

        return settings;
    }

    public async Task<UserSettingsDto> UpdateSettingsAsync(
        Guid userId,
        UpdateUserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var quota = GetQuota(user.Plan);

        var now = DateTime.UtcNow;
        var minScore = Math.Clamp(request.MinScore, 0, 100);
        var normalizedRegion = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim().ToUpperInvariant();
        var advancedFiltersEnabled = quota.AllowsAdvancedFilters && request.AdvancedFiltersEnabled;

        if (!quota.AllowsAdvancedFilters && request.AdvancedFiltersEnabled)
        {
            _logger.LogInformation(
                "Advanced filters request downgraded due to plan. UserId={UserId}, Plan={Plan}.",
                userId,
                user.Plan);
        }

        var existing = await _userSettingsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existing is null)
        {
            var created = new UserSettings(
                userId,
                request.Search ?? string.Empty,
                request.Source ?? string.Empty,
                minScore,
                request.VehicleType,
                normalizedRegion,
                advancedFiltersEnabled,
                now);

            await _userSettingsRepository.AddAsync(created, cancellationToken);
            await _userSettingsRepository.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Settings created. UserId={UserId}, Plan={Plan}, AdvancedFiltersEnabled={AdvancedFiltersEnabled}, ElapsedMs={ElapsedMs}.",
                userId,
                user.Plan,
                advancedFiltersEnabled,
                stopwatch.ElapsedMilliseconds);

            return MapSettings(created);
        }

        existing.Update(
            request.Search ?? string.Empty,
            request.Source ?? string.Empty,
            minScore,
            request.VehicleType,
            normalizedRegion,
            advancedFiltersEnabled,
            now);

        _userSettingsRepository.Update(existing);
        await _userSettingsRepository.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Settings updated. UserId={UserId}, Plan={Plan}, AdvancedFiltersEnabled={AdvancedFiltersEnabled}, ElapsedMs={ElapsedMs}.",
            userId,
            user.Plan,
            advancedFiltersEnabled,
            stopwatch.ElapsedMilliseconds);

        return MapSettings(existing);
    }

    private static OpportunityFeedItemDto MapOpportunity(LotDto lot)
    {
        var value = lot.CurrentBid ?? lot.FinalPrice ?? 0m;
        var summary = BuildSummary(lot);

        return new OpportunityFeedItemDto(
            lot.Id,
            lot.Source,
            lot.OpportunityScore,
            lot.OpportunityLabel,
            lot.Title,
            lot.Uf,
            value,
            lot.UpdatedAtUtc,
            summary,
            lot.RiskScore,
            lot.RiskDecision,
            lot.LotUrl,
            (int)lot.Status);
    }

    private static bool ContainsSearch(LotDto lot, string search)
    {
        var normalizedSearch = search.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return true;
        }

        return lot.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
               || lot.Source.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
               || lot.Auctioneer.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
               || lot.Model.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
               || lot.Make.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSummary(LotDto lot)
    {
        if (!string.IsNullOrWhiteSpace(lot.Description))
        {
            return lot.Description.Length <= 110
                ? lot.Description
                : lot.Description[..110].Trim() + "...";
        }

        return $"{lot.Make} {lot.Model} {lot.Year} em {lot.Uf}.";
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(userId, includeVehicles: false, cancellationToken)
               ?? throw new UnauthorizedAccessException("User not found for current token.");
    }

    private bool IsExactLotUrlForConnector(string? lotUrl)
    {
        if (!LotUrlGuard.IsValidLotUrl(lotUrl) || string.IsNullOrWhiteSpace(lotUrl) || !Uri.TryCreate(lotUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var connectors = _connectorFactory.CreateByDomain(uri.Host);
        if (connectors.Count == 0)
        {
            return true;
        }

        return connectors.Any(connector => connector.ValidateLotUrl(lotUrl));
    }

    private static ExtensionPlanQuota GetQuota(PlanType plan)
    {
        return plan switch
        {
            PlanType.Free => new ExtensionPlanQuota(
                MaxScannerRunsPerDay: 5,
                MaxOpportunityResults: 12,
                MaxHistoryItems: 12,
                AllowsAdvancedFilters: false,
                AllowsAdvancedConnectors: false),
            PlanType.Pro => new ExtensionPlanQuota(
                MaxScannerRunsPerDay: 20,
                MaxOpportunityResults: 40,
                MaxHistoryItems: 30,
                AllowsAdvancedFilters: false,
                AllowsAdvancedConnectors: false),
            PlanType.Premium => new ExtensionPlanQuota(
                MaxScannerRunsPerDay: 60,
                MaxOpportunityResults: 90,
                MaxHistoryItems: 80,
                AllowsAdvancedFilters: true,
                AllowsAdvancedConnectors: false),
            PlanType.Elite => new ExtensionPlanQuota(
                MaxScannerRunsPerDay: 180,
                MaxOpportunityResults: 180,
                MaxHistoryItems: 160,
                AllowsAdvancedFilters: true,
                AllowsAdvancedConnectors: true),
            _ => new ExtensionPlanQuota(5, 12, 12, false, false)
        };
    }

    private static bool IsAdvancedConnector(string source, string auctioneer)
    {
        var reference = $"{source} {auctioneer}".ToUpperInvariant();
        return reference.Contains("MEGA LEILOES", StringComparison.Ordinal)
               || reference.Contains("ZUKERMAN", StringComparison.Ordinal)
               || reference.Contains("PORTAL ZUK", StringComparison.Ordinal);
    }

    private async Task<UserSettingsDto> GetOrCreateSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _userSettingsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return MapSettings(existing);
        }

        var created = new UserSettings(
            userId,
            search: string.Empty,
            source: string.Empty,
            minScore: 60m,
            vehicleType: null,
            region: null,
            advancedFiltersEnabled: false,
            updatedAt: DateTime.UtcNow);

        await _userSettingsRepository.AddAsync(created, cancellationToken);
        await _userSettingsRepository.SaveChangesAsync(cancellationToken);

        return MapSettings(created);
    }

    private static UserSettingsDto MapSettings(UserSettings settings)
    {
        return new UserSettingsDto(
            Search: settings.Search,
            Source: settings.Source,
            MinScore: settings.MinScore,
            VehicleType: settings.VehicleType,
            Region: settings.Region,
            AdvancedFiltersEnabled: settings.AdvancedFiltersEnabled,
            UpdatedAtUtc: new DateTimeOffset(DateTime.SpecifyKind(settings.UpdatedAt, DateTimeKind.Utc)));
    }

    private sealed record ExtensionPlanQuota(
        int MaxScannerRunsPerDay,
        int MaxOpportunityResults,
        int MaxHistoryItems,
        bool AllowsAdvancedFilters,
        bool AllowsAdvancedConnectors);
}
