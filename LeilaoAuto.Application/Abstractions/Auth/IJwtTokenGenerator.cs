using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(User user);
}
