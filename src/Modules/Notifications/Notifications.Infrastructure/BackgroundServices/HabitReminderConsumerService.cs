using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.BackgroundServices;

public class HabitReminderConsumerService(
    IMessageConsumer consumer,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HabitReminderConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Habit Reminder Consumer Service starting");

        await consumer.Subscribe<HabitReminderCreated>(
            queueName: "notifications.habit-reminders",
            routingKey: MessagingConstants.HabitReminderCreatedKey,
            handler: async @event =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<HabitReminderCreated>>();
                await handler.HandleAsync(@event, stoppingToken);
            });

        await consumer.Subscribe<HabitReminderCancelled>(
            queueName: "notifications.habit-reminders-cancelled",
            routingKey: MessagingConstants.HabitReminderCancelledKey,
            handler: async @event =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<HabitReminderCancelled>>();
                await handler.HandleAsync(@event, stoppingToken);
            });
    }
}