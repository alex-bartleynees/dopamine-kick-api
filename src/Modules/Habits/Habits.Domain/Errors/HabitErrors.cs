using Common.Abstractions.Results;

namespace Habits.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.Habit"/> can produce, in one place.
/// </summary>
public static class HabitErrors
{
    public static Error NotFound(Guid habitId) =>
        Error.NotFound("Habits.NotFound", $"Habit with id {habitId} was not found");

    public static Error InvalidHabitIds(IEnumerable<Guid> habitIds) =>
        Error.Validation(
            "Habits.InvalidIds",
            $"The following habit IDs do not exist: {string.Join(", ", habitIds)}");
}
