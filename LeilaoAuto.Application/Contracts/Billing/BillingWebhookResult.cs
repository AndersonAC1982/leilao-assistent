namespace LeilaoAuto.Application.Contracts.Billing;

public sealed record BillingWebhookResult(bool Processed, string Message);
