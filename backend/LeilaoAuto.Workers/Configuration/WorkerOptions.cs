namespace LeilaoAuto.Workers.Configuration;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public bool Enabled { get; init; } = true;
    public int SyncIntervalSeconds { get; init; } = 180;
    public int ConnectorRetryCount { get; init; } = 3;
    public int ConnectorRetryDelaySeconds { get; init; } = 2;
    public bool RunOnStartup { get; init; } = true;
}
