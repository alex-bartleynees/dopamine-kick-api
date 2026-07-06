using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Quests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.BackgroundServices;

public class QuestReminderConsumerService(
    IMessageConsumer consumer,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QuestReminderConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Quest Reminder Consumer Service starting");

        await consumer.Subscribe<QuestReminderCreated>(
            queueName: "notifications.quest-reminders",
            routingKey: MessagingConstants.QuestReminderCreatedKey,
            handler: async @event =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<QuestReminderCreated>>();
                await handler.HandleAsync(@event, stoppingToken);
            });

        await consumer.Subscribe<QuestReminderCancelled>(
            queueName: "notifications.quest-reminders-cancelled",
            routingKey: MessagingConstants.QuestReminderCancelledKey,
            handler: async @event =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<QuestReminderCancelled>>();
                await handler.HandleAsync(@event, stoppingToken);
            });
    }
}
