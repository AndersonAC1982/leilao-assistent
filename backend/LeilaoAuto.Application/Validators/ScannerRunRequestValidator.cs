using FluentValidation;
using LeilaoAuto.Application.Contracts.Experience;

namespace LeilaoAuto.Application.Validators;

public sealed class ScannerRunRequestValidator : AbstractValidator<ScannerRunRequest>
{
    public ScannerRunRequestValidator()
    {
        RuleFor(request => request.Search)
            .MaximumLength(160);

        RuleFor(request => request.Category)
            .MaximumLength(60);

        RuleFor(request => request.Region)
            .MaximumLength(20);

        RuleFor(request => request.MinScore)
            .InclusiveBetween(0, 100);

        RuleFor(request => request.MaxPrice)
            .GreaterThan(0)
            .When(request => request.MaxPrice.HasValue);

        RuleFor(request => request.ActiveSources)
            .Must(sources => sources is null || sources.Count <= 20)
            .WithMessage("No máximo 20 fontes podem ser informadas por execução.");
    }
}
