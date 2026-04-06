using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LeilaoAutoDbContext _dbContext;

    public UserRepository(LeilaoAutoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid userId, bool includeVehicles, CancellationToken cancellationToken)
    {
        IQueryable<User> query = _dbContext.Users;

        if (includeVehicles)
        {
            query = query.Include(user => user.MonitoredVehicles);
        }

        return await query.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
