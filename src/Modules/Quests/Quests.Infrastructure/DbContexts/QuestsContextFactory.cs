using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Quests.Infrastructure.DbContexts;

public class QuestsContextFactory : IDesignTimeDbContextFactory<QuestsContext>
{
    public QuestsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<QuestsContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("QuestsDBConnectionString")
            ?? throw new InvalidOperationException("Connection string not found");

        var optionsBuilder = new DbContextOptionsBuilder<QuestsContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new QuestsContext(optionsBuilder.Options);
    }
}
