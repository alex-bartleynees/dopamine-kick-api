using SharedKernel.Results;

namespace Habits.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.HabitCompletion"/> can produce, in one place.
/// </summary>
public static class HabitCompletionErrors
{
    public static Error InvalidDayRange(int minDays, int maxDays) =>
        Error.Validation(
            "HabitCompletions.InvalidDayRange",
            $"'days' must be between {minDays} and {maxDays}.");

    public static Error InvalidTimezone(string timezone) =>
        Error.Validation(
            "HabitCompletions.InvalidTimezone",
            $"'{timezone}' is not a valid IANA timezone identifier.");
}
