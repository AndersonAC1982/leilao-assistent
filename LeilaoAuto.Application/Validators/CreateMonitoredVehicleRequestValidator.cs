using FluentValidation;
using LeilaoAuto.Application.Contracts.Monitoring;

namespace LeilaoAuto.Application.Validators;

public class CreateMonitoredVehicleRequestValidator : AbstractValidator<CreateMonitoredVehicleRequest>
{
    public CreateMonitoredVehicleRequestValidator()
    {
        RuleFor(request => request.Make)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.Model)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(request => request.YearFrom)
            .InclusiveBetween(1960, DateTime.UtcNow.Year + 1)
            .When(request => request.YearFrom.HasValue);

        RuleFor(request => request.YearTo)
            .InclusiveBetween(1960, DateTime.UtcNow.Year + 1)
            .When(request => request.YearTo.HasValue);

        RuleFor(request => request.YearTo)
            .GreaterThanOrEqualTo(request => request.YearFrom!.Value)
            .When(request => request.YearFrom.HasValue && request.YearTo.HasValue);

        RuleFor(request => request.Uf)
            .Length(2)
            .When(request => !string.IsNullOrWhiteSpace(request.Uf));
    }
}
