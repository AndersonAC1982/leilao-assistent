using LeilaoAuto.Application.Contracts.Auth;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
