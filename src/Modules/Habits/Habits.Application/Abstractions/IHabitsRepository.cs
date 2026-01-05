using Habits.Domain.Entities;

namespace Habits.Application.Abstractions;

public interface IHabitsRepository
{
    Task<List<Habit>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task CreateAsync(Habit habit, CancellationToken ct = default);
    Task CreateBulkAsync(List<Habit> habits, CancellationToken ct = default);
    Task<Habit?> GetHabitByIdAsync(Guid userId, Guid habitId, CancellationToken ct = default);
    Task CreateHabitCompletionAsync(HabitCompletion habitCompletion, CancellationToken ct = default);
}