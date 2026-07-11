using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Mediator;

namespace Habits.Application.Habits.Queries;

public record GetMyHabitCompletions(Guid UserId, int Days, string Timezone)
    : IRequest<Result<AllHabitCompletionHistoryDto>>;

public class GetMyHabitCompletionsHandler(IHabitsRepository habitsRepository)
    : IRequestHandler<GetMyHabitCompletions, Result<AllHabitCompletionHistoryDto>>
{
    public async ValueTask<Result<AllHabitCompletionHistoryDto>> Handle(GetMyHabitCompletions request,
        CancellationToken cancellationToken)
    {
        var window = CompletionHistoryWindow.Compute(request.Days, request.Timezone);
        if (window.IsFailure)
        {
            return window.Error;
        }

        var (from, to) = window.ValueOrThrow;
        var completions =
            await habitsRepository.GetCompletionDatesByUserAsync(request.UserId, from, to, cancellationToken);

        return new AllHabitCompletionHistoryDto(from, to, completions);
    }
}
