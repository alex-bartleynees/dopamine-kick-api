using SharedKernel.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Application.Common.Models;
using Quests.Domain.Entities;
using Quests.Domain.Errors;

namespace Quests.Application.Quests.Commands;

public record UpdateQuest(Guid UserId, Guid QuestId, QuestForUpdateDto Quest) : IRequest<Result<Quest>>;

public class UpdateQuestHandler(IQuestsRepository questsRepository, IQuestsUnitOfWork unitOfWork)
    : IRequestHandler<UpdateQuest, Result<Quest>>
{
    public async ValueTask<Result<Quest>> Handle(UpdateQuest request, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestByIdAsync(request.UserId, request.QuestId, cancellationToken);
        if (quest is null)
        {
            return Result<Quest>.Failure(QuestErrors.NotFound(request.QuestId));
        }

        quest.Emoji = request.Quest.Emoji;
        quest.Title = request.Quest.Title;
        quest.Description = request.Quest.Description;
        quest.DueAt = request.Quest.DueAt;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Quest>.Success(quest);
    }
}
