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

        RuleFor(request => request.Category)
            .NotEmpty()
            .MaximumLength(60);

        RuleForEach(request => request.ActiveSources)
            .NotEmpty()
            .MaximumLength(60)
            .When(request => request.ActiveSources is not null);

        RuleFor(request => request.ActiveSources)
            .Must(sources => sources is null || sources.Count <= 20)
            .WithMessage("No maximo 20 fontes podem ser configuradas por usuario.");

        RuleFor(request => request.MaxPrice)
            .GreaterThan(0)
            .When(request => request.MaxPrice.HasValue);
    }
}
