using SharedKernel.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;
using Quests.Domain.Errors;

namespace Quests.Application.Quests.Queries;

public record GetQuestById(Guid UserId, Guid QuestId) : IRequest<Result<Quest>>;

public class GetQuestByIdHandler(IQuestsRepository questsRepository) : IRequestHandler<GetQuestById, Result<Quest>>
{
    public async ValueTask<Result<Quest>> Handle(GetQuestById request, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestWithRemindersAsync(request.UserId, request.QuestId, cancellationToken);

        if (quest is null)
        {
            return Result<Quest>.Failure(QuestErrors.NotFound(request.QuestId));
        }

        return Result<Quest>.Success(quest);
    }
}
