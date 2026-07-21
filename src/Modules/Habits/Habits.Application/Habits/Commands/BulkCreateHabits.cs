using SharedKernel.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record BulkCreateHabits(Guid UserId, List<HabitForCreationDto> Habits) : IRequest<Result<List<Habit>>>;

public class BulkCreateHabitsHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork) : IRequestHandler<BulkCreateHabits, Result<List<Habit>>>
{
    private readonly IHabitsRepository _habitsRepository = habitsRepository;
    private readonly IHabitsUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result<List<Habit>>> Handle(BulkCreateHabits request, CancellationToken cancellationToken)
    {
        var habits = request.Habits.Select(dto => new Habit
        {
            UserId = request.UserId,
            Name = dto.Name,
            Emoji = dto.Emoji,
            Target = dto.Target
        }).ToList();

        await _habitsRepository.CreateBulkAsync(habits, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<List<Habit>>.Success(habits);
    }
}
