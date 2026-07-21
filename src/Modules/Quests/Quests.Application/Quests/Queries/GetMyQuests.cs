using SharedKernel.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;

namespace Quests.Application.Quests.Queries;

public record GetMyQuests(Guid UserId, QuestStatus? Status) : IRequest<Result<List<Quest>>>;

public class GetMyQuestsHandler(IQuestsRepository questsRepository) : IRequestHandler<GetMyQuests, Result<List<Quest>>>
{
    public async ValueTask<Result<List<Quest>>> Handle(GetMyQuests request, CancellationToken cancellationToken)
    {
        var quests = await questsRepository.GetByUserIdAsync(request.UserId, request.Status, cancellationToken);
        return Result<List<Quest>>.Success(quests);
    }
}
