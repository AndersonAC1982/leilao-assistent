using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IUserSettingsRepository : IBaseRepository<UserSettings>
{
    Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
