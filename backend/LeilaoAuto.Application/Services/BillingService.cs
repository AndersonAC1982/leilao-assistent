using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Billing;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeilaoAuto.Application.Services;

public class BillingService : IBillingService
{
    private static readonly IReadOnlyList<PlanDefinition> PlanDefinitions =
    [
        new(
            PlanType.Free,
            0m,
            [
                "Busca basica de lotes",
                "Monitoramento de ate 4 veiculos",
                "Sem analytics avancado"
            ]),
        new(
            PlanType.Pro,
            79.90m,
            [
                "Analytics completo",
                "Scoring de oportunidade e risco",
                "Historico comparativo por modelo"
            ]),
        new(
            PlanType.Premium,
            149.90m,
            [
                "Tudo do Pro",
                "Alertas e automacoes avancadas",
                "Prioridade em processamento"
            ]),
        new(
            PlanType.Elite,
            299.90m,
            [
                "Tudo do Premium",
                "Acesso ampliado multi-fonte",
                "Base preparada para API futura"
            ])
    ];

    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IBillingProvider _billingProvider;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IBillingProvider billingProvider,
        ILogger<BillingService> logger)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _billingProvider = billingProvider;
        _logger = logger;
    }

    public async Task<BillingPlanResponse> GetCurrentPlanAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: false, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found for current token.");

        var activeSubscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        _logger.LogInformation(
            "Billing plan requested for user {UserId}. CurrentPlan={CurrentPlan}, SubscriptionStatus={SubscriptionStatus}.",
            user.Id,
            user.Plan,
            activeSubscription?.Status);

        var plans = PlanDefinitions
            .Select(definition => new BillingPlanDetailsDto(
                definition.Plan,
                definition.Plan.ToDisplayName(),
                definition.MonthlyPrice,
                definition.Features))
            .ToList();

        return new BillingPlanResponse(
            user.Id,
            user.Plan,
            user.Plan.ToDisplayName(),
            activeSubscription?.Status,
            activeSubscription?.EndsAt,
            plans);
    }

    public async Task<BillingCheckoutResponse> StartCheckoutAsync(
        Guid userId,
        BillingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: false, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found for current token.");

        if (request.TargetPlan == user.Plan)
        {
            throw new InvalidOperationException("Target plan must be different from current plan.");
        }

        if (request.TargetPlan.Rank() < user.Plan.Rank())
        {
            throw new InvalidOperationException("Downgrade flow is not available in this phase.");
        }

        var checkoutSession = await _billingProvider.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequest(
                user.Id,
                user.Email,
                user.Plan,
                request.TargetPlan,
                request.SuccessUrl,
                request.CancelUrl),
            cancellationToken);

        _logger.LogInformation(
            "Checkout session created for user {UserId}. Provider={Provider}, CurrentPlan={CurrentPlan}, TargetPlan={TargetPlan}, SessionId={SessionId}.",
            user.Id,
            checkoutSession.Provider,
            user.Plan,
            request.TargetPlan,
            checkoutSession.SessionId);

        var pendingSubscription = new Subscription(
            user.Id,
            provider: checkoutSession.Provider,
            externalCustomerId: checkoutSession.ExternalCustomerId,
            externalSubscriptionId: checkoutSession.ExternalSubscriptionId,
            status: SubscriptionStatus.Pending,
            plan: request.TargetPlan,
            startedAt: DateTime.UtcNow,
            endsAt: null);

        await _subscriptionRepository.AddAsync(pendingSubscription, cancellationToken);
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);

        return new BillingCheckoutResponse(
            checkoutSession.Provider,
            checkoutSession.SessionId,
            checkoutSession.CheckoutUrl,
            request.TargetPlan,
            checkoutSession.ExpiresAtUtc,
            "Checkout created successfully.");
    }

    public async Task<BillingWebhookResult> HandleWebhookAsync(
        string payload,
        string? signature,
        CancellationToken cancellationToken)
    {
        var webhookEvent = await _billingProvider.ParseWebhookAsync(payload, signature, cancellationToken);
        if (webhookEvent is null)
        {
            _logger.LogInformation("Billing webhook ignored by provider {Provider}.", _billingProvider.Name);
            return new BillingWebhookResult(false, "Webhook ignored by current billing provider.");
        }

        var user = await _userRepository.GetByIdAsync(webhookEvent.UserId, includeVehicles: false, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "Billing webhook received for unknown user {UserId}. Provider={Provider}, EventType={EventType}.",
                webhookEvent.UserId,
                _billingProvider.Name,
                webhookEvent.EventType);
            return new BillingWebhookResult(false, "User not found for webhook payload.");
        }

        var subscription = await _subscriptionRepository.GetByExternalSubscriptionIdAsync(
            webhookEvent.ExternalSubscriptionId,
            cancellationToken);

        if (subscription is null)
        {
            subscription = new Subscription(
                user.Id,
                provider: _billingProvider.Name,
                externalCustomerId: webhookEvent.ExternalCustomerId,
                externalSubscriptionId: webhookEvent.ExternalSubscriptionId,
                status: webhookEvent.Status,
                plan: webhookEvent.Plan,
                startedAt: webhookEvent.OccurredAtUtc,
                endsAt: ResolveEndsAt(webhookEvent.Status, webhookEvent.OccurredAtUtc));

            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.UpdateFromProvider(
                webhookEvent.Status,
                webhookEvent.Plan,
                ResolveEndsAt(webhookEvent.Status, webhookEvent.OccurredAtUtc),
                webhookEvent.ExternalCustomerId);
            _subscriptionRepository.Update(subscription);
        }

        if (webhookEvent.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue)
        {
            user.ChangePlan(webhookEvent.Plan);
        }
        else if (webhookEvent.Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
        {
            user.ChangePlan(PlanType.Free);
        }

        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Billing webhook processed. User={UserId}, EventType={EventType}, NewPlan={Plan}, Status={Status}.",
            user.Id,
            webhookEvent.EventType,
            webhookEvent.Plan,
            webhookEvent.Status);

        return new BillingWebhookResult(true, $"Webhook processed for user {user.Email}.");
    }

    private static DateTime? ResolveEndsAt(SubscriptionStatus status, DateTime occurredAtUtc)
    {
        return status switch
        {
            SubscriptionStatus.Canceled => occurredAtUtc,
            SubscriptionStatus.Expired => occurredAtUtc,
            _ => occurredAtUtc.AddMonths(1)
        };
    }

    private sealed record PlanDefinition(
        PlanType Plan,
        decimal MonthlyPrice,
        IReadOnlyList<string> Features);
}
