using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Habits.Infrastructure.DbContexts;

public class HabitsContextFactory : IDesignTimeDbContextFactory<HabitsContext>
{
    public HabitsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<HabitsContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new HabitsContext(configuration);
    }
}