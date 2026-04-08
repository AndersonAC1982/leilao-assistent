using LeilaoAuto.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LeilaoAuto.Api.Authorization;

public sealed class MinimumPlanRequirement : IAuthorizationRequirement
{
    public MinimumPlanRequirement(PlanType minimumPlan)
    {
        MinimumPlan = minimumPlan;
    }

    public PlanType MinimumPlan { get; }
}
