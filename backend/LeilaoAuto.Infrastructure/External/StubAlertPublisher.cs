using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;

namespace LeilaoAuto.Infrastructure.External;

public class StubAlertPublisher : IAlertPublisher
{
    private readonly ILogger<StubAlertPublisher> _logger;

    public StubAlertPublisher(ILogger<StubAlertPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishOpportunityAsync(Guid userId, LotDto lot, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Alerta stub: usuário {UserId}, lote {LotNumber}, score oportunidade {OpportunityScore}.",
            userId,
            lot.LotNumber,
            lot.OpportunityScore);

        return Task.CompletedTask;
    }
}
