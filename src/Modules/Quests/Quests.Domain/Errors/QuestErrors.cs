using SharedKernel.Results;

namespace Quests.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.Quest"/> can produce, in one place.
/// </summary>
public static class QuestErrors
{
    public static Error NotFound(Guid questId) =>
        Error.NotFound("Quests.NotFound", $"Quest with id {questId} was not found");
}
