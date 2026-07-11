using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Mediator;

namespace Habits.Application.Habits.Queries;

public record GetHabitCompletions(Guid UserId, Guid HabitId, int Days, string Timezone)
    : IRequest<Result<HabitCompletionHistoryDto>>;

public class GetHabitCompletionsHandler(IHabitsRepository habitsRepository)
    : IRequestHandler<GetHabitCompletions, Result<HabitCompletionHistoryDto>>
{
    public async ValueTask<Result<HabitCompletionHistoryDto>> Handle(GetHabitCompletions request,
        CancellationToken cancellationToken)
    {
        var window = CompletionHistoryWindow.Compute(request.Days, request.Timezone);
        if (window.IsFailure)
        {
            return window.Error;
        }

        var habit = await habitsRepository.GetHabitByIdAsync(request.UserId, request.HabitId, cancellationToken);
        if (habit is null)
        {
            return new Error(404, "Not Found", $"Habit with id {request.HabitId} was not found");
        }

        var (from, to) = window.ValueOrThrow;
        var completions =
            await habitsRepository.GetCompletionDatesByHabitAsync(request.HabitId, from, to, cancellationToken);

        return new HabitCompletionHistoryDto(from, to, completions);
    }
}
