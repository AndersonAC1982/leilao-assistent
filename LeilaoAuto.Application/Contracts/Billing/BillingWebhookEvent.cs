using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingWebhookEvent(
    string EventType,
    Guid UserId,
    string ExternalCustomerId,
    string ExternalSubscriptionId,
    SubscriptionStatus Status,
    PlanType Plan,
    DateTime OccurredAtUtc);
