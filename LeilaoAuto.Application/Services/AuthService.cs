using LeilaoAuto.Application.Abstractions.Auth;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Auth;
using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("E-mail já cadastrado.");
        }

        var user = new User(normalizedEmail, _passwordHasher.Hash(request.Password));
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _jwtTokenGenerator.Generate(user);
        return new AuthResponse(user.Id, user.Email, token.Token, token.ExpiresAtUtc);
    }
}
