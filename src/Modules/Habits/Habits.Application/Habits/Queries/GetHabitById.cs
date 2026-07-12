using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Habits.Domain.Errors;
using Mediator;

namespace Habits.Application.Habits.Queries;

public record GetHabitById(Guid UserId, Guid HabitId) : IRequest<Result<Habit>>;

public class GetHabitByIdHandler(IHabitsRepository habitsRepository) : IRequestHandler<GetHabitById, Result<Habit>>
{
    public async ValueTask<Result<Habit>> Handle(GetHabitById request, CancellationToken cancellationToken)
    {
        var habit = await habitsRepository.GetHabitByIdAsync(request.UserId, request.HabitId, cancellationToken);

        if (habit is null)
        {
            return Result<Habit>.Failure(HabitErrors.NotFound(request.HabitId));
        }

        return Result<Habit>.Success(habit);
    }
}