using SharedKernel.Results;
using Mediator;
using Quests.Application.Abstractions;
using Quests.Application.Common.Models;
using Quests.Domain.Entities;

namespace Quests.Application.Quests.Commands;

public record CreateQuest(Guid UserId, QuestForCreationDto Quest) : IRequest<Result<Quest>>;

public class CreateQuestHandler(IQuestsRepository questsRepository, IQuestsUnitOfWork unitOfWork)
    : IRequestHandler<CreateQuest, Result<Quest>>
{
    public async ValueTask<Result<Quest>> Handle(CreateQuest request, CancellationToken cancellationToken)
    {
        var quest = new Quest
        {
            UserId = request.UserId,
            Emoji = request.Quest.Emoji,
            Title = request.Quest.Title,
            Description = request.Quest.Description,
            DueAt = request.Quest.DueAt,
            Status = QuestStatus.Pending
        };

        await questsRepository.CreateAsync(quest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Quest>.Success(quest);
    }
}
