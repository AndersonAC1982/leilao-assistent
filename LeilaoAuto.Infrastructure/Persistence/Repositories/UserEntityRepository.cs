using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class UserEntityRepository : BaseRepository<User>, IUserEntityRepository
{
    public UserEntityRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await DbContext.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
