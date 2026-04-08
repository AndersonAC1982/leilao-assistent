using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingPlanResponse(
    Guid UserId,
    PlanType CurrentPlan,
    string CurrentPlanDisplayName,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionEndsAt,
    IReadOnlyList<BillingPlanDetailsDto> Plans);
