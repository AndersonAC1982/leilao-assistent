using LeilaoAuto.Application.Contracts.Analytics;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IAnalyticsService
{
    Task<IReadOnlyList<ModelAveragePriceDto>> GetAveragePriceAsync(Guid userId, string? modelFilter, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpportunityDto>> GetOpportunitiesAsync(Guid userId, string? modelFilter, CancellationToken cancellationToken);
    Task<RiskSummaryDto> GetRiskSummaryAsync(Guid userId, string? modelFilter, CancellationToken cancellationToken);
}
