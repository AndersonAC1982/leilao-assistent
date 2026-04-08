using System.Text.Json;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Billing;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External;

public class FakeBillingProvider : IBillingProvider
{
    private readonly BillingProviderOptions _options;
    private readonly ILogger<FakeBillingProvider> _logger;

    public FakeBillingProvider(IOptions<BillingProviderOptions> options, ILogger<FakeBillingProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "Fake";

    public Task<BillingCheckoutSession> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = $"fake_session_{Guid.NewGuid():N}";
        var externalCustomerId = $"fake_customer_{request.UserId:N}";
        var externalSubscriptionId = $"fake_sub_{Guid.NewGuid():N}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        var successUrl = string.IsNullOrWhiteSpace(request.SuccessUrl) ? _options.SuccessUrl : request.SuccessUrl.Trim();
        var checkoutUrl = $"{successUrl}{(successUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?")}sessionId={sessionId}&plan={request.TargetPlan}";

        _logger.LogInformation(
            "Fake checkout generated for user {UserId} from {CurrentPlan} to {TargetPlan}. Session={SessionId}",
            request.UserId,
            request.CurrentPlan,
            request.TargetPlan,
            sessionId);

        return Task.FromResult(new BillingCheckoutSession(
            Provider: Name,
            SessionId: sessionId,
            CheckoutUrl: checkoutUrl,
            ExpiresAtUtc: expiresAtUtc,
            ExternalCustomerId: externalCustomerId,
            ExternalSubscriptionId: externalSubscriptionId));
    }

    public Task<BillingWebhookEvent?> ParseWebhookAsync(string payload, string? signature, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Task.FromResult<BillingWebhookEvent?>(null);
        }

        try
        {
            var webhookPayload = JsonSerializer.Deserialize<FakeWebhookPayload>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (webhookPayload is null || webhookPayload.UserId == Guid.Empty)
            {
                return Task.FromResult<BillingWebhookEvent?>(null);
            }

            if (!Enum.TryParse<PlanType>(webhookPayload.Plan, true, out var plan))
            {
                plan = PlanType.Free;
            }

            if (!Enum.TryParse<SubscriptionStatus>(webhookPayload.Status, true, out var status))
            {
                status = SubscriptionStatus.Pending;
            }

            var occurredAtUtc = webhookPayload.OccurredAtUtc ?? DateTime.UtcNow;
            var eventType = string.IsNullOrWhiteSpace(webhookPayload.EventType)
                ? "fake.subscription.updated"
                : webhookPayload.EventType.Trim();

            var externalCustomerId = string.IsNullOrWhiteSpace(webhookPayload.ExternalCustomerId)
                ? $"fake_customer_{webhookPayload.UserId:N}"
                : webhookPayload.ExternalCustomerId.Trim();

            var externalSubscriptionId = string.IsNullOrWhiteSpace(webhookPayload.ExternalSubscriptionId)
                ? $"fake_sub_{Guid.NewGuid():N}"
                : webhookPayload.ExternalSubscriptionId.Trim();

            return Task.FromResult<BillingWebhookEvent?>(new BillingWebhookEvent(
                eventType,
                webhookPayload.UserId,
                externalCustomerId,
                externalSubscriptionId,
                status,
                plan,
                occurredAtUtc));
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Fake billing webhook payload is invalid JSON.");
            return Task.FromResult<BillingWebhookEvent?>(null);
        }
    }

    private sealed record FakeWebhookPayload(
        string? EventType,
        Guid UserId,
        string? ExternalCustomerId,
        string? ExternalSubscriptionId,
        string? Status,
        string? Plan,
        DateTime? OccurredAtUtc);
}
