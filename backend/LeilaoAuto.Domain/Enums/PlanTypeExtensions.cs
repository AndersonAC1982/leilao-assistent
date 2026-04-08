namespace LeilaoAuto.Domain.Enums;

public static class PlanTypeExtensions
{
    public static int Rank(this PlanType plan)
    {
        return plan switch
        {
            PlanType.Free => 1,
            PlanType.Pro => 2,
            PlanType.Premium => 3,
            PlanType.Elite => 4,
            _ => 1
        };
    }

    public static bool IsAtLeast(this PlanType plan, PlanType minimumPlan)
    {
        return plan.Rank() >= minimumPlan.Rank();
    }

    public static string ToDisplayName(this PlanType plan)
    {
        return plan switch
        {
            PlanType.Free => "Free",
            PlanType.Pro => "Pro",
            PlanType.Premium => "Premium",
            PlanType.Elite => "Elite",
            _ => "Free"
        };
    }
}
