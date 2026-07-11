namespace Habits.Application.Common.Models;

public record HabitCompletionHistoryDto(DateOnly From, DateOnly To, List<DateOnly> Completions);

public record AllHabitCompletionHistoryDto(DateOnly From, DateOnly To, Dictionary<Guid, List<DateOnly>> Completions);
