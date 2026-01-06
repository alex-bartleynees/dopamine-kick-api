using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Common.Infrastructure.Messaging;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnection();
}

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options) : IRabbitMqConnectionFactory, IDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public async Task<IConnection> CreateConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }

    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        _connection?.Dispose();
        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}