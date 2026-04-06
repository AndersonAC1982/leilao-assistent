namespace LeilaoAuto.Application.Abstractions.Auth;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
