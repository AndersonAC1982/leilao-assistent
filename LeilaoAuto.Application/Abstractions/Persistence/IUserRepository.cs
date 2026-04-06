using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, bool includeVehicles, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
