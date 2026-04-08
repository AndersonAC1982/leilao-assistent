using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingCheckoutResponse(
    string Provider,
    string SessionId,
    string CheckoutUrl,
    PlanType TargetPlan,
    DateTime ExpiresAtUtc,
    string Message);
