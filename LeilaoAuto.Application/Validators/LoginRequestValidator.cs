using FluentValidation;
using LeilaoAuto.Application.Contracts.Auth;

namespace LeilaoAuto.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
