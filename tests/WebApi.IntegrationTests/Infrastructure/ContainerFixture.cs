using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace WebApi.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up the real infrastructure the API needs to boot — Postgres, RabbitMQ and Redis — once for
/// the whole test run and exposes connection settings for <see cref="CustomWebApplicationFactory"/>.
/// The app uses four databases on one Postgres server, so the extra three are created after start-up.
/// Keycloak is external and is faked at the auth layer instead (see <see cref="TestAuthHandler"/>).
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private static readonly string[] ModuleDatabases = ["usersdb", "habitsdb", "questsdb", "notificationsdb"];

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    /// <summary>Per-module Postgres connection strings keyed by database name (e.g. "habitsdb").</summary>
    public IReadOnlyDictionary<string, string> ConnectionStrings { get; private set; } =
        new Dictionary<string, string>();

    public string RabbitMqHost { get; private set; } = string.Empty;
    public int RabbitMqPort { get; private set; }
    public string RabbitMqUserName => "rabbitmq"; // Testcontainers RabbitMq default user
    public string RabbitMqPassword => "rabbitmq"; // Testcontainers RabbitMq default password
    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _rabbitMq.StartAsync(),
            _redis.StartAsync());

        var adminConnectionString = _postgres.GetConnectionString();
        await CreateModuleDatabasesAsync(adminConnectionString);

        ConnectionStrings = ModuleDatabases.ToDictionary(
            db => db,
            db => new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = db }.ConnectionString);

        RabbitMqHost = _rabbitMq.Hostname;
        RabbitMqPort = _rabbitMq.GetMappedPublicPort(5672);
        RedisConnectionString = _redis.GetConnectionString();
    }

    private static async Task CreateModuleDatabasesAsync(string adminConnectionString)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        foreach (var database in ModuleDatabases)
        {
            await using var command = connection.CreateCommand();
            // Database identifiers are hard-coded constants, so simple interpolation is safe here.
            command.CommandText = $"CREATE DATABASE \"{database}\"";
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _rabbitMq.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
    }
}
