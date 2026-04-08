using LeilaoAuto.Application.Contracts.Billing;

namespace LeilaoAuto.Application.Abstractions.External;

public interface IBillingProvider
{
    string Name { get; }
    Task<BillingCheckoutSession> CreateCheckoutSessionAsync(BillingCheckoutSessionRequest request, CancellationToken cancellationToken);
    Task<BillingWebhookEvent?> ParseWebhookAsync(string payload, string? signature, CancellationToken cancellationToken);
}
