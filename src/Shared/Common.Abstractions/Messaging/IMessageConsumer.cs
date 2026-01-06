namespace Common.Abstractions.Messaging;

public interface IMessageConsumer
{
    Task Subscribe<T>(string queueName, string routingKey, Func<T, Task> handler) 
        where T : class;
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}