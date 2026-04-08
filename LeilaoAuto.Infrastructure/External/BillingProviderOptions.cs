namespace LeilaoAuto.Infrastructure.External;

public sealed class BillingProviderOptions
{
    public const string SectionName = "Billing";

    public string Provider { get; init; } = "Fake";
    public string? StripeSecretKey { get; init; }
    public string? StripeWebhookSecret { get; init; }
    public string SuccessUrl { get; init; } = "http://localhost:4200/billing?checkout=success";
    public string CancelUrl { get; init; } = "http://localhost:4200/billing?checkout=cancel";
}
