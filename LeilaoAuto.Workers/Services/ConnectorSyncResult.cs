namespace LeilaoAuto.Workers.Services;

public sealed record ConnectorSyncResult(
    string ConnectorName,
    bool Success,
    int RecordsRead,
    int RecordsSaved,
    int Discarded,
    string Message);
