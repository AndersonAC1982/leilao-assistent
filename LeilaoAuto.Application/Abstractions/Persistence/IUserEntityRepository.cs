using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IUserEntityRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);
}
