using SharedKernel.Results;
using Habits.Domain.Errors;

namespace Habits.Application.Habits.Queries;

internal static class CompletionHistoryWindow
{
    internal const int MinDays = 1;
    internal const int MaxDays = 90;

    internal static Result<(DateOnly From, DateOnly To)> Compute(int days, string timezone)
    {
        if (days is < MinDays or > MaxDays)
        {
            return HabitCompletionErrors.InvalidDayRange(MinDays, MaxDays);
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out var userTimeZone))
        {
            return HabitCompletionErrors.InvalidTimezone(timezone);
        }

        var userLocalTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, userTimeZone);
        var to = DateOnly.FromDateTime(userLocalTime.DateTime);
        return (to.AddDays(-(days - 1)), to);
    }
}
