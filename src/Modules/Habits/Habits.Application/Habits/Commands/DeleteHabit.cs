using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Errors;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record DeleteHabit(Guid UserId, Guid HabitId) : IRequest<Result>;

public class DeleteHabitHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<DeleteHabit, Result>
{
    public async ValueTask<Result> Handle(DeleteHabit request, CancellationToken cancellationToken)
    {
        var habit = await habitsRepository.GetHabitByIdAsync(request.UserId, request.HabitId, cancellationToken);
        if (habit is null)
        {
            return Result.Failure(HabitErrors.NotFound(request.HabitId));
        }

        // Cancel the pushes for every reminder before the rows are cascade-deleted,
        // otherwise the self-contained Quartz jobs would keep firing.
        var reminders = await habitsRepository.GetRemindersByHabitAsync(request.UserId, request.HabitId, cancellationToken);
        var cancellationMessages = reminders
            .Select(HabitReminderCancellation.ToOutboxMessage)
            .ToList();

        if (cancellationMessages.Count > 0)
        {
            await habitsRepository.CreateBulkOutboxMessagesAsync(cancellationMessages, cancellationToken);
        }

        habitsRepository.Remove(habit);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
