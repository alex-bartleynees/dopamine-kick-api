using Common.Abstractions.Messaging;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Messaging;

public class IntegrationEventPublishHandler<TEvent>(
    IMessagePublisher messagePublisher,
    ILogger<IntegrationEventPublishHandler<TEvent>> logger)
    : INotificationHandler<TEvent>
    where TEvent : IntegrationEvent, INotification
{
    public async ValueTask Handle(TEvent notification, CancellationToken ct)
    {
        var routingKey = GetRoutingKey(notification);

        await messagePublisher.PublishAsync(notification, routingKey, ct);

        logger.LogInformation(
            "Published integration event {EventType} via Mediator handler",
            typeof(TEvent).Name);
    }

    private static string GetRoutingKey(INotification ev)
    {
        var eventType = ev.GetType();

        var attribute = eventType.GetCustomAttributes(typeof(IntegrationEventRoutingKeyAttribute), false)
            .FirstOrDefault() as IntegrationEventRoutingKeyAttribute;

        return attribute?.RoutingKey
               ?? throw new InvalidOperationException(
                   $"Integration event {eventType.Name} must have [IntegrationEventRoutingKey] attribute");
    }
}