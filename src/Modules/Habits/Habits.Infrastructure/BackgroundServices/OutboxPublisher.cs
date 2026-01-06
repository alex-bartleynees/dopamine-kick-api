using System.Text.Json;
using Habits.Domain.Entities;
using Habits.Infrastructure.DbContexts;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Habits.Infrastructure.BackgroundServices;

public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HabitsContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await context.OutboxMessages
            .Where(m => !m.Published)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var @event = DeserializeEvent(message);

                await mediator.Publish(@event, ct); 

                message.Published = true;
                message.PublishedAt = DateTimeOffset.UtcNow;

                logger.LogInformation(
                    "Published {Type} with MessageId {MessageId}",
                    message.Type,
                    message.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}", message.MessageId);
            }
        }

        await context.SaveChangesAsync(ct);
    }
    
    private INotification DeserializeEvent(OutboxMessage message)
    {
        var eventType = Type.GetType(message.Type)
                        ?? throw new InvalidOperationException($"Cannot resolve type: {message.Type}");

        if (!typeof(INotification).IsAssignableFrom(eventType))
            throw new InvalidOperationException($"{eventType.Name} must implement INotification");

        var @event = JsonSerializer.Deserialize(message.Payload, eventType)
                     ?? throw new InvalidOperationException("Deserialization failed");

        return (INotification)@event;
    }
}