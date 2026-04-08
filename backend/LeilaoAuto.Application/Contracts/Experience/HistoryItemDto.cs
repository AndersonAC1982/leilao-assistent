namespace LeilaoAuto.Application.Contracts.Experience;

public sealed record HistoryItemDto(
    Guid Id,
    string Source,
    DateTimeOffset ExecutedAtUtc,
    bool Success,
    int RecordsRead,
    int RecordsSaved,
    int NewLots,
    string Status,
    string? Message);
