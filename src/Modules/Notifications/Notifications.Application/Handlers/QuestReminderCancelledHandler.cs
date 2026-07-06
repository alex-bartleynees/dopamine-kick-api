using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Quests;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Handlers;

public class QuestReminderCancelledHandler(
    IProcessedMessageService processedMessageService,
    IJobScheduler jobScheduler,
    ILogger<QuestReminderCancelledHandler> logger)
    : IIntegrationEventHandler<QuestReminderCancelled>
{
    public async Task HandleAsync(QuestReminderCancelled @event, CancellationToken cancellationToken = default)
    {
        if (await processedMessageService.IsMessageProcessedAsync(@event.MessageId))
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", @event.MessageId);
            return;
        }

        await jobScheduler.CancelQuestReminderAsync(@event.ReminderId, @event.UserId);

        await processedMessageService.MarkAsProcessedAsync(
            @event.MessageId,
            nameof(QuestReminderCancelled));
    }
}
