using System.Text.Json;
using Common.Abstractions.Results;
using Common.IntegrationEvents.Habits;
using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record CreateHabitReminder(
    Guid HabitId,
    Guid UserId,
    TimeOnly NotificationTime,
    string TimeZone,
    string PreferredTime,
    bool IsEnabled) : IRequest<Result<Guid>>;

public class CreateHabitReminderHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<CreateHabitReminder, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateHabitReminder command, CancellationToken cancellationToken)
    {
        var habit = await habitsRepository.GetHabitByIdAsync(command.UserId, command.HabitId, cancellationToken);
        if (habit == null)
        {
            return Result<Guid>.Failure(new Error(404, "Not Found", $"Habit with id: {command.HabitId} was not found"));
        }
        
        var reminder = new HabitReminder
        {
            Id = Guid.NewGuid(),
            HabitId = command.HabitId,
            UserId = command.UserId,
            NotificationTime = command.NotificationTime,
            TimeZone = command.TimeZone,
            PreferredTime = command.PreferredTime,
            IsEnabled = command.IsEnabled
        };

        await habitsRepository.CreateReminderAsync(reminder, cancellationToken);

        if (reminder.IsEnabled)
        {
            var messageId = Guid.NewGuid();
            var outboxMessage = new OutboxMessage
            {
                MessageId = messageId,
                Type = typeof(HabitReminderCreated).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new HabitReminderCreated(
                    messageId,
                    reminder.Id,
                    reminder.UserId,
                    reminder.NotificationTime,
                    reminder.TimeZone,
                    habit.Name,
                    habit.Emoji,
                    habit.Target
                )),
                Published = false
            };

            await habitsRepository.CreateOutboxMessageAsync(outboxMessage, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reminder.Id;
    }
}