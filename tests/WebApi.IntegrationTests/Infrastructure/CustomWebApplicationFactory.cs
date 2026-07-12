using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real WebApi host against the containers from <see cref="ContainerFixture"/>. Only the
/// external dependency — Keycloak/JWT auth — is swapped for <see cref="TestAuthHandler"/>. Postgres,
/// RabbitMQ, Redis, the outbox publisher and Quartz all run for real so event flows are exercised.
///
/// Config is injected via environment variables rather than ConfigureAppConfiguration because the app's
/// RegisterServices reads connection strings and JWT settings during Program.Main — before the factory's
/// config callbacks are layered in. Environment variables are read by WebApplication.CreateBuilder up front,
/// so they are visible at registration time.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _originalEnv = new();

    private static readonly string[] EnvKeys =
    [
        "ConnectionStrings__UsersDBConnectionString",
        "ConnectionStrings__HabitsDBConnectionString",
        "ConnectionStrings__QuestsDBConnectionString",
        "ConnectionStrings__NotificationsDBConnectionString",
        "ConnectionStrings__RedisConnection",
        "RabbitMQ__HostName",
        "RabbitMQ__Port",
        "RabbitMQ__UserName",
        "RabbitMQ__Password",
        "RabbitMQ__VirtualHost",
        "Jwt__Authority",
        "Jwt__Audience",
        "Keycloak__BaseUrl",
        "Keycloak__Realm",
        "Keycloak__ClientId",
        "Keycloak__ClientSecret",
        "WebPush__Subject",
    ];

    public CustomWebApplicationFactory(ContainerFixture containers)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings__UsersDBConnectionString"] = containers.ConnectionStrings["usersdb"],
            ["ConnectionStrings__HabitsDBConnectionString"] = containers.ConnectionStrings["habitsdb"],
            ["ConnectionStrings__QuestsDBConnectionString"] = containers.ConnectionStrings["questsdb"],
            ["ConnectionStrings__NotificationsDBConnectionString"] = containers.ConnectionStrings["notificationsdb"],
            ["ConnectionStrings__RedisConnection"] = containers.RedisConnectionString,

            ["RabbitMQ__HostName"] = containers.RabbitMqHost,
            ["RabbitMQ__Port"] = containers.RabbitMqPort.ToString(),
            ["RabbitMQ__UserName"] = containers.RabbitMqUserName,
            ["RabbitMQ__Password"] = containers.RabbitMqPassword,
            ["RabbitMQ__VirtualHost"] = "/",

            // Present only so registration doesn't throw; auth is replaced so these are never contacted.
            ["Jwt__Authority"] = "https://test-authority.local/realms/test",
            ["Jwt__Audience"] = "account",
            ["Keycloak__BaseUrl"] = "https://test-authority.local",
            ["Keycloak__Realm"] = "test",
            ["Keycloak__ClientId"] = "test-client",
            ["Keycloak__ClientSecret"] = "test-secret",

            ["WebPush__Subject"] = "mailto:test@test.local",
        };

        foreach (var key in EnvKeys)
        {
            _originalEnv[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, values[key]);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" avoids loading appsettings.Development.json (which points at real external services).
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Make the test scheme the default so RequireAuthorization() authenticates against it
            // instead of the real JWT bearer handler.
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            foreach (var (key, value) in _originalEnv)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
