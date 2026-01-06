using System.Text.Json;
using Common.Abstractions.Results;
using Common.IntegrationEvents.Habits;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record BulkCreateHabitReminders(Guid UserId, List<HabitReminderForCreationDto> Reminders) : IRequest<Result<List<Guid>>>;

public class BulkCreateHabitRemindersHandler : IRequestHandler<BulkCreateHabitReminders, Result<List<Guid>>>
{
    private readonly IHabitsRepository _habitsRepository;
    private readonly IHabitsUnitOfWork _unitOfWork;

    public BulkCreateHabitRemindersHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    {
        _habitsRepository = habitsRepository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<List<Guid>>> Handle(BulkCreateHabitReminders request, CancellationToken cancellationToken)
    {
        var reminders = request.Reminders.Select(dto => new HabitReminder
        {
            Id = Guid.NewGuid(),
            HabitId = dto.HabitId,
            UserId = request.UserId,
            NotificationTime = dto.NotificationTime,
            TimeZone = dto.Timezone,
            PreferredTime = dto.PreferredTime,
            IsEnabled = dto.isEnabled
        }).ToList();

        var outboxMessages = reminders.Where(r => r.IsEnabled).Select(reminder =>
        {
            var messageId = Guid.NewGuid();
            return new OutboxMessage
            {
                MessageId = messageId,
                Type = nameof(HabitReminderCreated),
                Payload = JsonSerializer.Serialize(new HabitReminderCreated(
                    messageId,
                    reminder.Id,
                    reminder.UserId,
                    reminder.NotificationTime,
                    reminder.TimeZone
                )),
                Published = false
            };
        }).ToList();

        await _habitsRepository.CreateBulkRemindersAsync(reminders, cancellationToken);

        if (outboxMessages.Count > 0)
        {
            await _habitsRepository.CreateBulkOutboxMessagesAsync(outboxMessages, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<List<Guid>>.Success(reminders.Select(r => r.Id).ToList());
    }
}
