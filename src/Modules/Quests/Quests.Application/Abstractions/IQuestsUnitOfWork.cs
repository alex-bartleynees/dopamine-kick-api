namespace Quests.Application.Abstractions;

public interface IQuestsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
