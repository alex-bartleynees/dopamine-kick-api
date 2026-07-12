using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Errors;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record DeleteHabitReminder(Guid UserId, Guid ReminderId) : IRequest<Result>;

public class DeleteHabitReminderHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<DeleteHabitReminder, Result>
{
    public async ValueTask<Result> Handle(DeleteHabitReminder request, CancellationToken cancellationToken)
    {
        var reminder = await habitsRepository.GetReminderByIdAsync(request.UserId, request.ReminderId, cancellationToken);
        if (reminder is null)
        {
            return Result.Failure(HabitReminderErrors.NotFound(request.ReminderId));
        }

        // Unschedule the push (no-op if no job exists), then remove the reminder.
        await habitsRepository.CreateOutboxMessageAsync(
            HabitReminderCancellation.ToOutboxMessage(reminder), cancellationToken);

        habitsRepository.RemoveReminder(reminder);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
