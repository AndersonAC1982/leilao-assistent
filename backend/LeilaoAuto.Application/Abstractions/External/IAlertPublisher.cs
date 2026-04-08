using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Abstractions.External;

public interface IAlertPublisher
{
    Task PublishOpportunityAsync(Guid userId, LotDto lot, CancellationToken cancellationToken);
}
