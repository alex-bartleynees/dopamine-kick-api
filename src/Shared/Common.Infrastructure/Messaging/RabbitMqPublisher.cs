using System.Runtime.CompilerServices;
using System.Text.Json;
using Common.Abstractions.Messaging;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Common.Infrastructure.Messaging;



public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly AsyncLazy<IChannel> _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private int _disposed;

    public RabbitMqPublisher(
        IRabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _channel = new AsyncLazy<IChannel>(async () =>
        {
            var connection = await connectionFactory.CreateConnection();
            
            var channelOptions = new CreateChannelOptions(true, true);
            var channel = await connection.CreateChannelAsync(channelOptions);

            await channel.ExchangeDeclareAsync(exchange: MessagingConstants.ExchangeName,
                type: MessagingConstants.ExchangeType, durable: true, autoDelete: false);

            return channel;
        });
    }

    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
        where T : class
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
        {
            throw new ObjectDisposedException(nameof(RabbitMqPublisher));
        }

        var channel = await _channel;

        if (!channel.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ channel is closed");
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: MessagingConstants.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
        
        _logger.LogInformation(
            "Published message to exchange {Exchange} with routing key {RoutingKey}",
            MessagingConstants.ExchangeName,
            routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        if (_channel.IsValueCreated)
        {
            var channel = await _channel;
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
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
    }
}
