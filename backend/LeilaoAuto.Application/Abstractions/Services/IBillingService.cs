using LeilaoAuto.Application.Contracts.Billing;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IBillingService
{
    Task<BillingPlanResponse> GetCurrentPlanAsync(Guid userId, CancellationToken cancellationToken);
    Task<BillingCheckoutResponse> StartCheckoutAsync(Guid userId, BillingCheckoutRequest request, CancellationToken cancellationToken);
    Task<BillingWebhookResult> HandleWebhookAsync(string payload, string? signature, CancellationToken cancellationToken);
}
