using SharedKernel.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record CreateHabit(Guid UserId, HabitForCreationDto Habit) : IRequest<Result<Habit>>;

public class CreateHabitHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork) : IRequestHandler<CreateHabit, Result<Habit>>
{
    private readonly IHabitsRepository _habitsRepository = habitsRepository;
    private readonly IHabitsUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result<Habit>> Handle(CreateHabit request, CancellationToken cancellationToken)
    {
        var habit = new Habit
        {
            UserId = request.UserId,
            Name = request.Habit.Name,
            Emoji = request.Habit.Emoji,
            Target = request.Habit.Target
        };

        await _habitsRepository.CreateAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Habit>.Success(habit);
    }
}
