using LeilaoAuto.Application.Contracts.Experience;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IExperienceService
{
    Task<IReadOnlyList<OpportunityFeedItemDto>> GetOpportunitiesAsync(
        Guid userId,
        OpportunityFeedQueryRequest request,
        CancellationToken cancellationToken);

    Task<ScannerRunResponseDto> RunScannerAsync(
        Guid userId,
        ScannerRunRequest? request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryItemDto>> GetHistoryAsync(Guid userId, int take, CancellationToken cancellationToken);

    Task<UserSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserSettingsDto> UpdateSettingsAsync(
        Guid userId,
        UpdateUserSettingsRequest request,
        CancellationToken cancellationToken);
}
