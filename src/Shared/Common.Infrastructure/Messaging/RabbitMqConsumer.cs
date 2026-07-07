using System.Text;
using System.Text.Json;
using Common.Abstractions.Messaging;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Infrastructure.Messaging;

public class RabbitMqConsumer : IMessageConsumer, IAsyncDisposable
{
  private readonly ILogger<RabbitMqConsumer> _logger;
  private readonly RabbitMqOptions _options;
  private readonly AsyncLazy<IChannel> _channel;
  private readonly List<string> _consumerTags = new();
  private readonly SemaphoreSlim _subscribeLock = new(1, 1);
  private int _disposed;

  private const string RetryCountHeader = "x-retry-count";

  public RabbitMqConsumer(
      IRabbitMqConnectionFactory connectionFactory,
      IOptions<RabbitMqOptions> options,
      ILogger<RabbitMqConsumer> logger)
  {
    _logger = logger;
    _options = options.Value;
    _channel = new AsyncLazy<IChannel>(async () =>
    {
      var connection = await connectionFactory.CreateConnection();
      var channel = await connection.CreateChannelAsync();

      await channel.ExchangeDeclareAsync(
              exchange: MessagingConstants.ExchangeName,
              type: MessagingConstants.ExchangeType,
              durable: true,
              autoDelete: false
          );

      // Declare Dead Letter Exchange
      await channel.ExchangeDeclareAsync(
              exchange: MessagingConstants.DeadLetterExchangeName,
              type: "direct",
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

      // Declare Dead Letter Queue
      var dlqName = queueName + MessagingConstants.DeadLetterQueueSuffix;
      await channel.QueueDeclareAsync(
          queue: dlqName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: null
      );

      // Bind DLQ to DLX
      await channel.QueueBindAsync(
          queue: dlqName,
          exchange: MessagingConstants.DeadLetterExchangeName,
          routingKey: routingKey
      );

      // Declare main queue with DLX configured
      var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", MessagingConstants.DeadLetterExchangeName },
                { "x-dead-letter-routing-key", routingKey }
            };

      await channel.QueueDeclareAsync(
          queue: queueName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: queueArgs
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
        var retryCount = GetRetryCount(ea.BasicProperties);

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

            // Permanent failure - send to DLQ
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
          }
        }
        catch (JsonException ex)
        {
          _logger.LogError(ex,
                    "JSON deserialization error from queue {Queue}. Body length: {Length}",
                    queueName,
                    body.Length);

          // Permanent failure - malformed message, send to DLQ
          await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex,
                    "Error processing message from queue {Queue}, RetryCount={RetryCount}/{MaxRetries}",
                    queueName,
                    retryCount,
                    _options.MaxRetryCount);

          // Check if we should retry
          if (retryCount < _options.MaxRetryCount)
          {
            // Requeue with incremented retry count
            await RequeueMessageWithDelay(channel, ea, body, retryCount + 1, routingKey);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

            _logger.LogInformation(
                      "Message requeued for retry {RetryCount}/{MaxRetries} from queue {Queue}",
                      retryCount + 1,
                      _options.MaxRetryCount,
                      queueName);
          }
          else
          {
            // Max retries exceeded - send to DLQ via nack
            _logger.LogWarning(
                      "Max retries exceeded for message from queue {Queue}. Sending to DLQ",
                      queueName);

            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
          }
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

  private int GetRetryCount(IReadOnlyBasicProperties? properties)
  {
    if (properties?.Headers == null)
      return 0;

    if (properties.Headers.TryGetValue(RetryCountHeader, out var value))
    {
      return value switch
      {
        int intValue => intValue,
        byte[] bytes => BitConverter.ToInt32(bytes, 0),
        _ => 0
      };
    }

    return 0;
  }

  private async Task RequeueMessageWithDelay(
      IChannel channel,
      BasicDeliverEventArgs ea,
      byte[] body,
      int newRetryCount,
      string routingKey)
  {
    var properties = new BasicProperties
    {
      Persistent = true,
      Headers = new Dictionary<string, object?>
            {
                { RetryCountHeader, newRetryCount }
            }
    };

    // Copy existing headers if they exist
    if (ea.BasicProperties.Headers != null)
    {
      foreach (var header in ea.BasicProperties.Headers)
      {
        if (header.Key != RetryCountHeader)
        {
          properties.Headers[header.Key] = header.Value;
        }
      }
    }

    // Publish back to the exchange with the routing key
    // The delay can be implemented using a delayed queue plugin or by waiting before republishing
    // For simplicity, we'll wait before republishing
    if (_options.RetryDelayMs > 0)
    {
      await Task.Delay(_options.RetryDelayMs);
    }

    await channel.BasicPublishAsync(
        exchange: MessagingConstants.ExchangeName,
        routingKey: routingKey,
        mandatory: false,
        basicProperties: properties,
        body: body
    );
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
