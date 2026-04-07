using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Auth;

public sealed record AuthMeResponse(
    Guid UserId,
    string Email,
    UserRole Role,
    PlanType Plan,
    DateTime CreatedAt);
