using FluentValidation;
using LeilaoAuto.Application.Contracts.Experience;

namespace LeilaoAuto.Application.Validators;

public sealed class UpdateUserSettingsRequestValidator : AbstractValidator<UpdateUserSettingsRequest>
{
    public UpdateUserSettingsRequestValidator()
    {
        RuleFor(request => request.Search)
            .MaximumLength(120);

        RuleFor(request => request.Source)
            .MaximumLength(80);

        RuleFor(request => request.MinScore)
            .InclusiveBetween(0, 100);

        RuleFor(request => request.VehicleType)
            .GreaterThan(0)
            .When(request => request.VehicleType.HasValue);

        RuleFor(request => request.Region)
            .MaximumLength(10)
            .When(request => !string.IsNullOrWhiteSpace(request.Region));
    }
}
