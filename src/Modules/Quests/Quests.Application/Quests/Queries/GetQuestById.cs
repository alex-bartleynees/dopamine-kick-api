using Common.Abstractions.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;

namespace Quests.Application.Quests.Queries;

public record GetQuestById(Guid UserId, Guid QuestId) : IRequest<Result<Quest>>;

public class GetQuestByIdHandler(IQuestsRepository questsRepository) : IRequestHandler<GetQuestById, Result<Quest>>
{
    public async ValueTask<Result<Quest>> Handle(GetQuestById request, CancellationToken cancellationToken)
    {
        var quest = await questsRepository.GetQuestWithRemindersAsync(request.UserId, request.QuestId, cancellationToken);

        if (quest is null)
        {
            return Result<Quest>.Failure(new Error(404, "Not Found", $"Quest with id {request.QuestId} was not found"));
        }

        return Result<Quest>.Success(quest);
    }
}
