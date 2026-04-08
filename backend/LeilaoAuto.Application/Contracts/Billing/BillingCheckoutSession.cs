namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingCheckoutSession(
    string Provider,
    string SessionId,
    string CheckoutUrl,
    DateTime ExpiresAtUtc,
    string ExternalCustomerId,
    string ExternalSubscriptionId);
