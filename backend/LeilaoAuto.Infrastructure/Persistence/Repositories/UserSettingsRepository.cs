using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class UserSettingsRepository : BaseRepository<UserSettings>, IUserSettingsRepository
{
    public UserSettingsRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return DbContext.UserSettings.FirstOrDefaultAsync(settings => settings.UserId == userId, cancellationToken);
    }
}
