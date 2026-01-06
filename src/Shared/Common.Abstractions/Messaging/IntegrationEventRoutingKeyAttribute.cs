namespace Common.Abstractions.Messaging;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class IntegrationEventRoutingKeyAttribute : Attribute
{
    public string RoutingKey { get; }

    public IntegrationEventRoutingKeyAttribute(string routingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        RoutingKey = routingKey;
    }
}
