using SharedKernel.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Queries;

public record GetMyHabits(Guid UserId) : IRequest<Result<List<Habit>>>;

public class GetMyHabitsHandler(IHabitsRepository habitsRepository) : IRequestHandler<GetMyHabits, Result<List<Habit>>>
{
    private readonly IHabitsRepository _habitsRepository = habitsRepository;

    public async ValueTask<Result<List<Habit>>> Handle(GetMyHabits request, CancellationToken cancellationToken)
    {
        var habits = await _habitsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result<List<Habit>>.Success(habits);
    }
}
