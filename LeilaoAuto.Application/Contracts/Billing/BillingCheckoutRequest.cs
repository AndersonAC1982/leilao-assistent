using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingCheckoutRequest(
    PlanType TargetPlan,
    string? SuccessUrl,
    string? CancelUrl);
