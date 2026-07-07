using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record UpdateHabit(Guid UserId, Guid HabitId, HabitForUpdateDto Habit) : IRequest<Result<Habit>>;

public class UpdateHabitHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<UpdateHabit, Result<Habit>>
{
    public async ValueTask<Result<Habit>> Handle(UpdateHabit request, CancellationToken cancellationToken)
    {
        var habit = await habitsRepository.GetHabitByIdAsync(request.UserId, request.HabitId, cancellationToken);
        if (habit is null)
        {
            return Result<Habit>.Failure(new Error(404, "Not Found", $"Habit with id {request.HabitId} was not found"));
        }

        habit.Name = request.Habit.Name;
        habit.Emoji = request.Habit.Emoji;
        habit.Target = request.Habit.Target;

        // Refresh scheduled reminders so their push text reflects the new habit details.
        var reminders = await habitsRepository.GetRemindersByHabitAsync(request.UserId, request.HabitId, cancellationToken);
        var refreshMessages = reminders
            .Where(r => r.IsEnabled)
            .Select(r => HabitReminderScheduling.ToOutboxMessage(r, habit))
            .ToList();

        if (refreshMessages.Count > 0)
        {
            await habitsRepository.CreateBulkOutboxMessagesAsync(refreshMessages, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Habit>.Success(habit);
    }
}
