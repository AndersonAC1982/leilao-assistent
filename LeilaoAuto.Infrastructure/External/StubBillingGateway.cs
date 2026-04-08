using LeilaoAuto.Application.Abstractions.External;
using Microsoft.Extensions.Logging;

namespace LeilaoAuto.Infrastructure.External;

public class StubBillingGateway : IBillingGateway
{
    private readonly ILogger<StubBillingGateway> _logger;

    public StubBillingGateway(ILogger<StubBillingGateway> logger)
    {
        _logger = logger;
    }

    public Task RegisterSearchAsync(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Billing stub captured search event for user {UserId}.", userId);
        return Task.CompletedTask;
    }
}
