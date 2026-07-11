using Common.Abstractions.Results;

namespace Habits.Application.Habits.Queries;

internal static class CompletionHistoryWindow
{
    internal const int MinDays = 1;
    internal const int MaxDays = 90;

    internal static Result<(DateOnly From, DateOnly To)> Compute(int days, string timezone)
    {
        if (days is < MinDays or > MaxDays)
        {
            return new Error(400, "Bad Request", $"'days' must be between {MinDays} and {MaxDays}.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out var userTimeZone))
        {
            return new Error(400, "Bad Request", $"'{timezone}' is not a valid IANA timezone identifier.");
        }

        var userLocalTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, userTimeZone);
        var to = DateOnly.FromDateTime(userLocalTime.DateTime);
        return (to.AddDays(-(days - 1)), to);
    }
}
