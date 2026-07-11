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

    public void Remove(Habit habit)
    {
        context.Habits.Remove(habit);
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

    public async Task<HabitReminder?> GetReminderByIdAsync(Guid userId, Guid reminderId, CancellationToken ct = default)
    {
        return await context.HabitReminders
            .Where(r => r.UserId == userId && r.Id == reminderId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<HabitReminder>> GetRemindersByHabitAsync(Guid userId, Guid habitId, CancellationToken ct = default)
    {
        return await context.HabitReminders
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.HabitId == habitId)
            .ToListAsync(ct);
    }

    public void RemoveReminder(HabitReminder reminder)
    {
        context.HabitReminders.Remove(reminder);
    }

    public async Task CreateOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddAsync(message, ct);
    }

    public async Task CreateBulkOutboxMessagesAsync(List<OutboxMessage> messages, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddRangeAsync(messages, ct);
    }

    public async Task<Dictionary<Guid, List<DateOnly>>> GetCompletionDatesByUserAsync(Guid userId, DateOnly from,
        DateOnly to, CancellationToken ct = default)
    {
        var habits = await context.Habits
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .Select(h => new
            {
                h.Id,
                Dates = context.HabitCompletions
                    .Where(c => c.HabitId == h.Id && c.CompletedDate >= from && c.CompletedDate <= to)
                    .OrderBy(c => c.CompletedDate)
                    .Select(c => c.CompletedDate)
                    .ToList()
            })
            .ToListAsync(ct);

        return habits.ToDictionary(h => h.Id, h => h.Dates);
    }

    public async Task<List<DateOnly>> GetCompletionDatesByHabitAsync(Guid habitId, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        return await context.HabitCompletions
            .AsNoTracking()
            .Where(c => c.HabitId == habitId && c.CompletedDate >= from && c.CompletedDate <= to)
            .OrderBy(c => c.CompletedDate)
            .Select(c => c.CompletedDate)
            .ToListAsync(ct);
    }
}