using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Billing;

namespace LeilaoAuto.Infrastructure.External;

/// <summary>
/// TODO(Stripe):
/// 1) Integrar SDK oficial Stripe (Checkout Session + assinatura recorrente).
/// 2) Validar assinatura do webhook com Stripe-Signature.
/// 3) Mapear eventos customer.subscription.* e checkout.session.completed.
/// 4) Persistir IDs externos reais e sincronizar lifecycle completo.
/// </summary>
public class StripeBillingProvider : IBillingProvider
{
    public string Name => "Stripe";

    public Task<BillingCheckoutSession> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Stripe provider is not implemented yet. Configure Billing:Provider=Fake for this phase.");
    }

    public Task<BillingWebhookEvent?> ParseWebhookAsync(string payload, string? signature, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Stripe provider is not implemented yet. Configure Billing:Provider=Fake for this phase.");
    }
}
