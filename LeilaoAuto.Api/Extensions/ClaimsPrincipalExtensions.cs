using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace LeilaoAuto.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedAccessException("Token JWT sem identificador de usuário válido.");
        }

        return userId;
    }
}
