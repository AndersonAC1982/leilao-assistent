using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingCheckoutSessionRequest(
    Guid UserId,
    string Email,
    PlanType CurrentPlan,
    PlanType TargetPlan,
    string? SuccessUrl,
    string? CancelUrl);
