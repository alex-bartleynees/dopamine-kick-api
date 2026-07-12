using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Errors;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record UpdateHabitReminder(Guid UserId, Guid ReminderId, HabitReminderForUpdateDto Reminder)
    : IRequest<Result<Guid>>;

public class UpdateHabitReminderHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<UpdateHabitReminder, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(UpdateHabitReminder request, CancellationToken cancellationToken)
    {
        var reminder = await habitsRepository.GetReminderByIdAsync(request.UserId, request.ReminderId, cancellationToken);
        if (reminder is null)
        {
            return Result<Guid>.Failure(HabitReminderErrors.NotFound(request.ReminderId));
        }

        reminder.NotificationTime = request.Reminder.NotificationTime;
        reminder.TimeZone = request.Reminder.TimeZone;
        reminder.PreferredTime = request.Reminder.PreferredTime;
        reminder.IsEnabled = request.Reminder.IsEnabled;

        if (reminder.IsEnabled)
        {
            var habit = await habitsRepository.GetHabitByIdAsync(request.UserId, reminder.HabitId, cancellationToken);
            if (habit is null)
            {
                return Result<Guid>.Failure(HabitErrors.NotFound(reminder.HabitId));
            }

            // Enabled: (re)schedule with the new time/text.
            await habitsRepository.CreateOutboxMessageAsync(
                HabitReminderScheduling.ToOutboxMessage(reminder, habit), cancellationToken);
        }
        else
        {
            // Disabled: unschedule any existing job.
            await habitsRepository.CreateOutboxMessageAsync(
                HabitReminderCancellation.ToOutboxMessage(reminder), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reminder.Id;
    }
}
