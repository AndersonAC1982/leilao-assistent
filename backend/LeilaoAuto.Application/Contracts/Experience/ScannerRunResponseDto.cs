namespace LeilaoAuto.Application.Contracts.Experience;

public sealed record ScannerRunResponseDto(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int RefreshedLots,
    bool Success,
    string Message);
