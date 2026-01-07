using System.Text.Json;
using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Logging;

namespace Notifications.Application.Handlers;

public class HabitReminderCreatedHandler(ILogger<HabitReminderCreatedHandler> logger)
    : IIntegrationEventHandler<HabitReminderCreated>
{
    public async Task HandleAsync(HabitReminderCreated @event, CancellationToken cancellationToken = default)
    {
        var eventDetails = JsonSerializer.Serialize(@event, new JsonSerializerOptions { WriteIndented = true });
        logger.LogInformation("Processing HabitReminderCreated event: {EventDetails}", eventDetails);
        logger.LogInformation($"{@event.HabitEmoji} Time to complete your '{@event.HabitName}' habit!");

        // TODO: Implement actual notification logic (email, push, SMS, etc.)

        await Task.CompletedTask;
    }
}
