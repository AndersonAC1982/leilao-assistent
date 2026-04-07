namespace LeilaoAuto.Workers.Services;

public interface ILotBackgroundSyncProcessor
{
    Task<LotSyncCycleResult> RunCycleAsync(CancellationToken cancellationToken);
}
