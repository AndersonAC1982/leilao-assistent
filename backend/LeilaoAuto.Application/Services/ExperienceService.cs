using System.Collections.Concurrent;
using System.Text.Json;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Application.Services;

public sealed class ExperienceService : IExperienceService
{
    // TODO: persist user settings in dedicated table (currently in-memory scaffold for extension-first flow).
    private static readonly ConcurrentDictionary<Guid, UserSettingsDto> UserSettingsStore = new();

    private readonly ILotService _lotService;
    private readonly IConnectorExecutionLogRepository _connectorExecutionLogRepository;

    public ExperienceService(
        ILotService lotService,
        IConnectorExecutionLogRepository connectorExecutionLogRepository)
    {
        _lotService = lotService;
        _connectorExecutionLogRepository = connectorExecutionLogRepository;
    }

    public async Task<IReadOnlyList<OpportunityFeedItemDto>> GetOpportunitiesAsync(
        Guid userId,
        OpportunityFeedQueryRequest request,
        CancellationToken cancellationToken)
    {
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
            return [];
        }

        var opportunities = searchResult.ActiveLots
            .Where(lot => LotUrlGuard.IsValidLotUrl(lot.LotUrl))
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
            .ToList();

        return opportunities;
    }

    public async Task<ScannerRunResponseDto> RunScannerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            var refreshed = await _lotService.RefreshAsync(cancellationToken);
            var completedAt = DateTimeOffset.UtcNow;

            var payload = JsonSerializer.Serialize(new
            {
                userId,
                refreshed,
                startedAt,
                completedAt,
                source = "api/scanner/run"
            });

            await _connectorExecutionLogRepository.AddAsync(
                new ConnectorExecutionLog(
                    connectorName: "ScannerManual",
                    executedAt: completedAt.UtcDateTime,
                    success: true,
                    recordsRead: refreshed,
                    recordsSaved: refreshed,
                    message: "Manual scanner run from extension/web/mobile facade.",
                    payloadJson: payload),
                cancellationToken);

            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);

            return new ScannerRunResponseDto(
                startedAt,
                completedAt,
                refreshed,
                true,
                $"Varredura concluida com sucesso. {refreshed} lotes atualizados.");
        }
        catch (Exception exception)
        {
            var completedAt = DateTimeOffset.UtcNow;
            var payload = JsonSerializer.Serialize(new
            {
                userId,
                startedAt,
                completedAt,
                source = "api/scanner/run",
                error = exception.Message
            });

            await _connectorExecutionLogRepository.AddAsync(
                new ConnectorExecutionLog(
                    connectorName: "ScannerManual",
                    executedAt: completedAt.UtcDateTime,
                    success: false,
                    recordsRead: 0,
                    recordsSaved: 0,
                    message: exception.Message,
                    payloadJson: payload),
                cancellationToken);

            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);

            return new ScannerRunResponseDto(
                startedAt,
                completedAt,
                0,
                false,
                "A varredura falhou. Verifique os conectores e tente novamente.");
        }
    }

    public async Task<IReadOnlyList<HistoryItemDto>> GetHistoryAsync(int take, CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 30);
        var logs = await _connectorExecutionLogRepository.GetRecentAsync(safeTake, cancellationToken);

        return logs
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
    }

    public Task<UserSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = UserSettingsStore.GetOrAdd(userId, _ => DefaultSettings());
        return Task.FromResult(settings);
    }

    public Task<UserSettingsDto> UpdateSettingsAsync(
        Guid userId,
        UpdateUserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var updated = new UserSettingsDto(
            request.Search?.Trim() ?? string.Empty,
            request.Source?.Trim() ?? string.Empty,
            Math.Clamp(request.MinScore, 0, 100),
            request.VehicleType,
            string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim().ToUpperInvariant(),
            request.AdvancedFiltersEnabled,
            DateTimeOffset.UtcNow);

        UserSettingsStore.AddOrUpdate(userId, updated, (_, _) => updated);
        return Task.FromResult(updated);
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

    private static UserSettingsDto DefaultSettings()
    {
        return new UserSettingsDto(
            Search: string.Empty,
            Source: string.Empty,
            MinScore: 60m,
            VehicleType: null,
            Region: null,
            AdvancedFiltersEnabled: false,
            UpdatedAtUtc: DateTimeOffset.UtcNow);
    }
}
