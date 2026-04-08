using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LeilaoAuto.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedAccessException("JWT token without a valid user identifier.");
        }

        return userId;
    }
}
