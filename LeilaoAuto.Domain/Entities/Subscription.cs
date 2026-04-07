using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Domain.Entities;

public class Subscription
{
    private Subscription()
    {
    }

    public Subscription(
        Guid userId,
        string provider,
        string externalCustomerId,
        string externalSubscriptionId,
        SubscriptionStatus status,
        PlanType plan,
        DateTime startedAt,
        DateTime? endsAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider.Trim();
        ExternalCustomerId = externalCustomerId.Trim();
        ExternalSubscriptionId = externalSubscriptionId.Trim();
        Status = status;
        Plan = plan;
        StartedAt = startedAt;
        EndsAt = endsAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalCustomerId { get; private set; } = string.Empty;
    public string ExternalSubscriptionId { get; private set; } = string.Empty;
    public SubscriptionStatus Status { get; private set; }
    public PlanType Plan { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public User? User { get; private set; }
}
