using System.Text.Json;
using Common.Abstractions.Messaging;
using Habits.Domain.Entities;
using Habits.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Habits.Infrastructure.BackgroundServices;

public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher messagePublisher,
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

        var messages = await context.OutboxMessages
            .Where(m => !m.Published)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var (@event, routingKey) = DeserializeEvent(message);

                await messagePublisher.PublishAsync(@event, routingKey, ct);

                message.Published = true;
                message.PublishedAt = DateTimeOffset.UtcNow;

                logger.LogInformation(
                    "Published {Type} with MessageId {MessageId} to RabbitMQ with routing key {RoutingKey}",
                    message.Type,
                    message.MessageId,
                    routingKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}", message.MessageId);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private (object Event, string RoutingKey) DeserializeEvent(OutboxMessage message)
    {
        var eventType = Type.GetType(message.Type)
                        ?? throw new InvalidOperationException($"Cannot resolve type: {message.Type}");

        var @event = JsonSerializer.Deserialize(message.Payload, eventType)
                     ?? throw new InvalidOperationException("Deserialization failed");

        var routingKey = GetRoutingKey(eventType);

        return (@event, routingKey);
    }

    private static string GetRoutingKey(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(IntegrationEventRoutingKeyAttribute), false)
            .FirstOrDefault() as IntegrationEventRoutingKeyAttribute;

        return attribute?.RoutingKey
               ?? throw new InvalidOperationException(
                   $"Integration event {eventType.Name} must have [IntegrationEventRoutingKey] attribute");
    }
}