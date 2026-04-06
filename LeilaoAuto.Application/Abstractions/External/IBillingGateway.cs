namespace LeilaoAuto.Application.Abstractions.External;

public interface IBillingGateway
{
    Task RegisterSearchAsync(Guid userId, CancellationToken cancellationToken);
}
