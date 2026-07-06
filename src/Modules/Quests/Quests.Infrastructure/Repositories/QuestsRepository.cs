using Microsoft.EntityFrameworkCore;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;
using Quests.Infrastructure.DbContexts;

namespace Quests.Infrastructure.Repositories;

public class QuestsRepository(QuestsContext context) : IQuestsRepository
{
    public async Task<List<Quest>> GetByUserIdAsync(Guid userId, QuestStatus? status = null, CancellationToken ct = default)
    {
        var query = context.Quests
            .AsNoTracking()
            .Where(q => q.UserId == userId);

        if (status is not null)
        {
            query = query.Where(q => q.Status == status);
        }

        return await query
            .OrderBy(q => q.DueAt)
            .ToListAsync(ct);
    }

    public async Task<Quest?> GetQuestByIdAsync(Guid userId, Guid questId, CancellationToken ct = default)
    {
        return await context.Quests
            .Where(q => q.UserId == userId && q.Id == questId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Quest?> GetQuestWithRemindersAsync(Guid userId, Guid questId, CancellationToken ct = default)
    {
        return await context.Quests
            .Include(q => q.Reminders)
            .Where(q => q.UserId == userId && q.Id == questId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(Quest quest, CancellationToken ct = default)
    {
        await context.Quests.AddAsync(quest, ct);
    }

    public void Remove(Quest quest)
    {
        context.Quests.Remove(quest);
    }

    public async Task CreateReminderAsync(QuestReminder reminder, CancellationToken ct = default)
    {
        await context.QuestReminders.AddAsync(reminder, ct);
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
