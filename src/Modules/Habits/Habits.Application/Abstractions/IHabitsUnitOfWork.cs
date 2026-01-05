namespace Habits.Application.Abstractions;

public interface IHabitsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
