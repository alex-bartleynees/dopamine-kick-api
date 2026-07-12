using Common.Abstractions.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;
using Quests.Domain.Errors;

namespace Quests.Application.Quests.Commands;

public record CompleteQuest(Guid UserId, Guid QuestId) : IRequest<Result<Quest>>;

public class CompleteQuestHandler(IQuestsRepository questsRepository, IQuestsUnitOfWork unitOfWork)
    : IRequestHandler<CompleteQuest, Result<Quest>>
{
    public async ValueTask<Result<Quest>> Handle(CompleteQuest request, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestWithRemindersAsync(request.UserId, request.QuestId, cancellationToken);
        if (quest is null)
        {
            return Result<Quest>.Failure(QuestErrors.NotFound(request.QuestId));
        }

        if (quest.Status == QuestStatus.Completed)
        {
            return Result<Quest>.Success(quest);
        }

        quest.Status = QuestStatus.Completed;
        quest.CompletedAt = DateTimeOffset.UtcNow;

        var cancellationMessages = quest.Reminders
            .Select(reminder => QuestReminderCancellation.ToOutboxMessage(reminder))
            .ToList();

        if (cancellationMessages.Count > 0)
        {
            await questsRepository.CreateBulkOutboxMessagesAsync(cancellationMessages, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Quest>.Success(quest);
    }
}
