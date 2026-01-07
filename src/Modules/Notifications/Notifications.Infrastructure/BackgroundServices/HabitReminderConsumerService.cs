using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.BackgroundServices;

public class HabitReminderConsumerService(
    IMessageConsumer consumer,
    IIntegrationEventHandler<HabitReminderCreated> handler,
    ILogger<HabitReminderConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Habit Reminder Consumer Service starting");

        await consumer.Subscribe<HabitReminderCreated>(
            queueName: "notifications.habit-reminders",
            routingKey: MessagingConstants.HabitReminderCreatedKey,
            handler: async @event => await handler.HandleAsync(@event, stoppingToken));
    }
}