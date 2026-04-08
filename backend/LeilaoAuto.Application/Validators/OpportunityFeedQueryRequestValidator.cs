using FluentValidation;
using LeilaoAuto.Application.Contracts.Experience;

namespace LeilaoAuto.Application.Validators;

public sealed class OpportunityFeedQueryRequestValidator : AbstractValidator<OpportunityFeedQueryRequest>
{
    public OpportunityFeedQueryRequestValidator()
    {
        RuleFor(request => request.Search)
            .MaximumLength(120)
            .When(request => !string.IsNullOrWhiteSpace(request.Search));

        RuleFor(request => request.Source)
            .MaximumLength(80)
            .When(request => !string.IsNullOrWhiteSpace(request.Source));

        RuleFor(request => request.MinScore)
            .InclusiveBetween(0, 100)
            .When(request => request.MinScore.HasValue);

        RuleFor(request => request.VehicleType)
            .GreaterThan(0)
            .When(request => request.VehicleType.HasValue);

        RuleFor(request => request.Region)
            .MaximumLength(10)
            .When(request => !string.IsNullOrWhiteSpace(request.Region));

        RuleFor(request => request.Uf)
            .Length(2)
            .When(request => !string.IsNullOrWhiteSpace(request.Uf));

        RuleFor(request => request.Year)
            .InclusiveBetween(1960, DateTime.UtcNow.Year + 1)
            .When(request => request.Year.HasValue);
    }
}
