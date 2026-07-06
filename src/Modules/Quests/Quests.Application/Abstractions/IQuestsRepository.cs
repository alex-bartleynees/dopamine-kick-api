using Quests.Domain.Entities;

namespace Quests.Application.Abstractions;

public interface IQuestsRepository
{
    Task<List<Quest>> GetByUserIdAsync(Guid userId, QuestStatus? status = null, CancellationToken ct = default);
    Task<Quest?> GetQuestByIdAsync(Guid userId, Guid questId, CancellationToken ct = default);
    Task<Quest?> GetQuestWithRemindersAsync(Guid userId, Guid questId, CancellationToken ct = default);
    Task CreateAsync(Quest quest, CancellationToken ct = default);
    void Remove(Quest quest);
    Task CreateReminderAsync(QuestReminder reminder, CancellationToken ct = default);
    Task CreateOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default);
    Task CreateBulkOutboxMessagesAsync(List<OutboxMessage> messages, CancellationToken ct = default);
}
