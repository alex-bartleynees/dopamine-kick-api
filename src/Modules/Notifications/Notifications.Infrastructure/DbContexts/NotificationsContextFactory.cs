using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Notifications.Infrastructure.DbContexts;

public class NotificationsContextFactory : IDesignTimeDbContextFactory<NotificationsContext>
{
    public NotificationsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<NotificationsContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("NotificationsDBConnectionString")
            ?? throw new InvalidOperationException("Connection string not found");

        var optionsBuilder = new DbContextOptionsBuilder<NotificationsContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NotificationsContext(optionsBuilder.Options);
    }
}