using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Quests;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Handlers;

public class QuestReminderCreatedHandler(
    IProcessedMessageService processedMessageService,
    IJobScheduler jobScheduler,
    ILogger<QuestReminderCreatedHandler> logger)
    : IIntegrationEventHandler<QuestReminderCreated>
{
    public async Task HandleAsync(QuestReminderCreated @event, CancellationToken cancellationToken = default)
    {
        if (await processedMessageService.IsMessageProcessedAsync(@event.MessageId))
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", @event.MessageId);
            return;
        }

        await jobScheduler.ScheduleQuestReminderAsync(@event);

        await processedMessageService.MarkAsProcessedAsync(
            @event.MessageId,
            nameof(QuestReminderCreated));
    }
}
