using FluentValidation;
using LeilaoAuto.Application.Contracts.Billing;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Validators;

public class BillingCheckoutRequestValidator : AbstractValidator<BillingCheckoutRequest>
{
    public BillingCheckoutRequestValidator()
    {
        RuleFor(request => request.TargetPlan)
            .Must(plan => plan is PlanType.Pro or PlanType.Premium or PlanType.Elite)
            .WithMessage("Target plan must be Pro, Premium or Elite.");

        RuleFor(request => request.SuccessUrl)
            .MaximumLength(800)
            .Must(BeValidUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.SuccessUrl))
            .WithMessage("SuccessUrl must be a valid absolute URL.");

        RuleFor(request => request.CancelUrl)
            .MaximumLength(800)
            .Must(BeValidUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.CancelUrl))
            .WithMessage("CancelUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }
}
