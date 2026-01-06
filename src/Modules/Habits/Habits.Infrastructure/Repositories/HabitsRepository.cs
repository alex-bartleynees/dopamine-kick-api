using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Habits.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Habits.Infrastructure.Repositories;

public class HabitsRepository(HabitsContext context) : IHabitsRepository
{
    public async Task<List<Habit>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Habits
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task CreateAsync(Habit habit, CancellationToken ct = default)
    {
        await context.Habits.AddAsync(habit, ct);
    }

    public async Task CreateBulkAsync(List<Habit> habits, CancellationToken ct = default)
    {
        await context.Habits.AddRangeAsync(habits, ct);
    }

    public async Task<Habit?> GetHabitByIdAsync(Guid userId, Guid habitId, CancellationToken ct = default)
    {
        return await context.Habits
            .Where(h => h.UserId == userId && h.Id == habitId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task CreateHabitCompletionAsync(HabitCompletion habitCompletion, CancellationToken ct = default)
    {
        await context.HabitCompletions.AddAsync(habitCompletion, ct);
    }

    public async Task CreateReminderAsync(HabitReminder reminder, CancellationToken ct = default)
    {
        await context.HabitReminders.AddAsync(reminder, ct);
    }

    public async Task CreateBulkRemindersAsync(List<HabitReminder> reminders, CancellationToken ct = default)
    {
        await context.HabitReminders.AddRangeAsync(reminders, ct);
    }

    public async Task CreateOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddAsync(message, ct);
    }

    public async Task CreateBulkOutboxMessagesAsync(List<OutboxMessage> messages, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddRangeAsync(messages, ct);
    }
}