using System.Text.Json;
using Common.Abstractions.Results;
using Common.IntegrationEvents.Habits;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Entities;
using Habits.Domain.Errors;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record BulkCreateHabitReminders(Guid UserId, List<BulkHabitReminderItemDto> Reminders) : IRequest<Result<List<Guid>>>;

public class BulkCreateHabitRemindersHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<BulkCreateHabitReminders, Result<List<Guid>>>
{
    public async ValueTask<Result<List<Guid>>> Handle(BulkCreateHabitReminders request, CancellationToken cancellationToken)
    {
        var userHabits = await habitsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var habitsById = userHabits.ToDictionary(h => h.Id);
        var invalidHabitIds = request.Reminders.Select(r => r.HabitId)
            .Where(habitId => !habitsById.ContainsKey(habitId)).ToList();

        if (invalidHabitIds.Count != 0)
        {
            return Result<List<Guid>>.Failure(HabitErrors.InvalidHabitIds(invalidHabitIds));
        }

        var reminders = request.Reminders.Select(dto => new HabitReminder
        {
            Id = Guid.NewGuid(),
            HabitId = dto.HabitId,
            UserId = request.UserId,
            NotificationTime = dto.NotificationTime,
            TimeZone = dto.TimeZone,
            PreferredTime = dto.PreferredTime,
            IsEnabled = dto.IsEnabled
        }).ToList();

        var outboxMessages = reminders.Where(r => r.IsEnabled).Select(reminder =>
        {
            var habit = habitsById[reminder.HabitId];
            var messageId = Guid.NewGuid();
            return new OutboxMessage
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
        }).ToList();

        await habitsRepository.CreateBulkRemindersAsync(reminders, cancellationToken);

        if (outboxMessages.Count > 0)
        {
            await habitsRepository.CreateBulkOutboxMessagesAsync(outboxMessages, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<List<Guid>>.Success(reminders.Select(r => r.Id).ToList());
    }
}
