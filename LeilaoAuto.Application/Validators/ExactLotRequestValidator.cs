using FluentValidation;
using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Validators;

public class ExactLotRequestValidator : AbstractValidator<ExactLotRequest>
{
    public ExactLotRequestValidator()
    {
        RuleFor(request => request.Auctioneer)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(request => request.LotNumber)
            .NotEmpty()
            .MaximumLength(30);
    }
}
