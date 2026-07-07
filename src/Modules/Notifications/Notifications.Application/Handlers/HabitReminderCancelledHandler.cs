using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Handlers;

public class HabitReminderCancelledHandler(
    IProcessedMessageService processedMessageService,
    IJobScheduler jobScheduler,
    ILogger<HabitReminderCancelledHandler> logger)
    : IIntegrationEventHandler<HabitReminderCancelled>
{
    public async Task HandleAsync(HabitReminderCancelled @event, CancellationToken cancellationToken = default)
    {
        if (await processedMessageService.IsMessageProcessedAsync(@event.MessageId))
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", @event.MessageId);
            return;
        }

        await jobScheduler.CancelHabitReminderAsync(@event.ReminderId, @event.UserId);

        await processedMessageService.MarkAsProcessedAsync(
            @event.MessageId,
            nameof(HabitReminderCancelled));
    }
}
