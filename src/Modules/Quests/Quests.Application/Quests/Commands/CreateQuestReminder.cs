using System.Text.Json;
using Common.Abstractions.Results;
using Common.IntegrationEvents.Quests;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;

namespace Quests.Application.Quests.Commands;

public record CreateQuestReminder(
    Guid UserId,
    Guid QuestId,
    DateTimeOffset RemindAt,
    string TimeZone,
    bool IsEnabled) : IRequest<Result<QuestReminder>>;

public class CreateQuestReminderHandler(IQuestsRepository questsRepository, IQuestsUnitOfWork unitOfWork)
    : IRequestHandler<CreateQuestReminder, Result<QuestReminder>>
{
    public async ValueTask<Result<QuestReminder>> Handle(CreateQuestReminder command, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestByIdAsync(command.UserId, command.QuestId, cancellationToken);
        if (quest is null)
        {
            return Result<QuestReminder>.Failure(
                new Error(404, "Not Found", $"Quest with id {command.QuestId} was not found"));
        }

        if (quest.Status == QuestStatus.Completed)
        {
            return Result<QuestReminder>.Failure(
                new Error(400, "BadRequest", "Cannot add a reminder to a completed quest"));
        }

        var reminder = new QuestReminder
        {
            Id = Guid.NewGuid(),
            QuestId = command.QuestId,
            UserId = command.UserId,
            RemindAt = command.RemindAt,
            TimeZone = command.TimeZone,
            IsEnabled = command.IsEnabled
        };

        await questsRepository.CreateReminderAsync(reminder, cancellationToken);

        if (reminder.IsEnabled)
        {
            var messageId = Guid.NewGuid();
            var outboxMessage = new OutboxMessage
            {
                MessageId = messageId,
                Type = typeof(QuestReminderCreated).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new QuestReminderCreated(
                    messageId,
                    reminder.Id,
                    quest.Id,
                    reminder.UserId,
                    reminder.RemindAt,
                    reminder.TimeZone,
                    quest.Title,
                    quest.Emoji)),
                Published = false
            };

            await questsRepository.CreateOutboxMessageAsync(outboxMessage, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<QuestReminder>.Success(reminder);
    }
}
