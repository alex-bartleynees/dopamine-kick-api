using Habits.Domain.Entities;

namespace Habits.Application.Abstractions;

public interface IHabitsRepository
{
    Task<List<Habit>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task CreateAsync(Habit habit, CancellationToken ct = default);
    Task CreateBulkAsync(List<Habit> habits, CancellationToken ct = default);
    Task<Habit?> GetHabitByIdAsync(Guid userId, Guid habitId, CancellationToken ct = default);
    void Remove(Habit habit);
    Task CreateHabitCompletionAsync(HabitCompletion habitCompletion, CancellationToken ct = default);
    Task CreateReminderAsync(HabitReminder reminder, CancellationToken ct = default);
    Task CreateBulkRemindersAsync(List<HabitReminder> reminders, CancellationToken ct = default);
    Task<HabitReminder?> GetReminderByIdAsync(Guid userId, Guid reminderId, CancellationToken ct = default);
    Task<List<HabitReminder>> GetRemindersByHabitAsync(Guid userId, Guid habitId, CancellationToken ct = default);
    void RemoveReminder(HabitReminder reminder);
    Task CreateOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default);
    Task CreateBulkOutboxMessagesAsync(List<OutboxMessage> messages, CancellationToken ct = default);
}