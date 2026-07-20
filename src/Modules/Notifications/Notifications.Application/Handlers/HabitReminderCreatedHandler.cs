using SharedKernel.Messaging.Abstractions;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Handlers;

public class HabitReminderCreatedHandler(
    IProcessedMessageService processedMessageService,
    IJobScheduler jobScheduler,
    ILogger<HabitReminderCreatedHandler> logger)
    : IIntegrationEventHandler<HabitReminderCreated>
{
    public async Task HandleAsync(HabitReminderCreated @event, CancellationToken cancellationToken = default)
    {
        if (await processedMessageService.IsMessageProcessedAsync(@event.MessageId))
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", @event.MessageId);
            return;
        }

        await jobScheduler.ScheduleHabitReminderAsync(@event);

        await processedMessageService.MarkAsProcessedAsync(
            @event.MessageId,
            nameof(HabitReminderCreated));
    }
}