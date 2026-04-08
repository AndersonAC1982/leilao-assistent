using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingPlanDetailsDto(
    PlanType Plan,
    string DisplayName,
    decimal MonthlyPrice,
    IReadOnlyList<string> Features);
