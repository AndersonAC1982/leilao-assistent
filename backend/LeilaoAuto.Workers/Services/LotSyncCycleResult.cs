namespace LeilaoAuto.Workers.Services;

public sealed record LotSyncCycleResult(
    int ConnectorsProcessed,
    int RecordsRead,
    int RecordsSaved,
    int AnalyticsUpdated,
    int Failures,
    IReadOnlyList<ConnectorSyncResult> ConnectorResults);
