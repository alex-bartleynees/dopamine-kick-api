using SharedKernel.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Errors;

namespace Quests.Application.Quests.Commands;

public record DeleteQuest(Guid UserId, Guid QuestId) : IRequest<Result>;

public class DeleteQuestHandler(IQuestsRepository questsRepository, IQuestsUnitOfWork unitOfWork)
    : IRequestHandler<DeleteQuest, Result>
{
    public async ValueTask<Result> Handle(DeleteQuest request, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestWithRemindersAsync(request.UserId, request.QuestId, cancellationToken);
        if (quest is null)
        {
            return Result.Failure(QuestErrors.NotFound(request.QuestId));
        }

        var cancellationMessages = quest.Reminders
            .Select(QuestReminderCancellation.ToOutboxMessage)
            .ToList();

        if (cancellationMessages.Count > 0)
        {
            await questsRepository.CreateBulkOutboxMessagesAsync(cancellationMessages, cancellationToken);
        }

        questsRepository.Remove(quest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
