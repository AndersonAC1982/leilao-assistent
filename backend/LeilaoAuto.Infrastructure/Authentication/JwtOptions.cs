namespace LeilaoAuto.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "LeilaoAuto";
    public string Audience { get; init; } = "LeilaoAuto.Web";
    public string SecretKey { get; init; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET";
    public int ExpiresMinutes { get; init; } = 120;
}
