using FluentValidation;
using LeilaoAuto.Application.Contracts.Monitoring;

namespace LeilaoAuto.Application.Validators;

public class UpdateMonitoredVehicleRequestValidator : AbstractValidator<UpdateMonitoredVehicleRequest>
{
    public UpdateMonitoredVehicleRequestValidator()
    {
        var maxYear = DateTime.UtcNow.Year + 1;

        RuleFor(request => request.Brand)
            .NotEmpty()
            .MaximumLength(60);

        RuleFor(request => request.Model)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Year)
            .InclusiveBetween(1960, maxYear);

        RuleFor(request => request.Type)
            .IsInEnum();

        RuleFor(request => request.Uf)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$");

        RuleFor(request => request.VehicleState)
            .IsInEnum();
    }
}
