using System.Text;
using System.Text.Json;
using Common.Abstractions.Messaging;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Infrastructure.Messaging;

public class RabbitMqConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly AsyncLazy<IChannel> _channel;
    private readonly List<string> _consumerTags = new();
    private readonly SemaphoreSlim _subscribeLock = new(1, 1);
    private int _disposed;

    RabbitMqConsumer(IRabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqConsumer> logger)
    {
        var connectionFactory1 = connectionFactory;
        _logger = logger;

        _channel = new AsyncLazy<IChannel>(async () =>
        {
            var connection = await connectionFactory1.CreateConnection();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: MessagingConstants.ExchangeName,
                type: MessagingConstants.ExchangeType,
                durable: true,
                autoDelete: false
            );
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 10,
                global: false
            );

            return channel;
        });
    }

    public async Task Subscribe<T>(string queueName, string routingKey, Func<T, Task> handler) where T : class
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
        {
            throw new ObjectDisposedException(nameof(RabbitMqConsumer));
        }

        await _subscribeLock.WaitAsync();
        try
        {
            var channel = await _channel;

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: MessagingConstants.ExchangeName,
                routingKey: routingKey
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();

                try
                {
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message);

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                        _logger.LogInformation(
                            "Processed message from queue {Queue}, DeliveryTag={DeliveryTag}",
                            queueName,
                            ea.DeliveryTag);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Null message after deserialization from queue {Queue}",
                            queueName);

                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex,
                        "JSON deserialization error from queue {Queue}. Body length: {Length}",
                        queueName,
                        body.Length);

                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing message from queue {Queue}",
                        queueName);

                    // Reject without requeue (will go to DLX if configured)
                    // Set requeue: true if you want to retry transient errors
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            var consumerTag = await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer);

            _consumerTags.Add(consumerTag);

            _logger.LogInformation(
                "Subscribed to queue {Queue} with routing key {RoutingKey}, ConsumerTag={ConsumerTag}",
                queueName,
                routingKey,
                consumerTag);
        }
        finally
        {
            _subscribeLock.Release();
        }
    }


    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _channel;
        _logger.LogInformation("RabbitMQ consumer started");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.IsValueCreated)
        {
            var channel = await _channel;
            
            foreach (var tag in _consumerTags)
            {
                try
                {
                    await channel.BasicCancelAsync(tag, cancellationToken: cancellationToken);
                    _logger.LogInformation("Cancelled consumer {ConsumerTag}", tag);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cancelling consumer {ConsumerTag}", tag);
                }
            }
        }
        
        _logger.LogInformation("RabbitMQ consumer stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await StopAsync();

        if (_channel.IsValueCreated)
        {
            var channel = await _channel;
            try
            {
                if (channel.IsOpen)
                    await channel.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing channel");
            }
            finally
            {
                channel.Dispose();
            }
        }

        _subscribeLock.Dispose();
        GC.SuppressFinalize(this);
    }
}