using Microsoft.EntityFrameworkCore;
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

        var connectionString = configuration.GetConnectionString("HabitsDBConnectionString")
            ?? throw new InvalidOperationException("Connection string not found");

        var optionsBuilder = new DbContextOptionsBuilder<HabitsContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new HabitsContext(optionsBuilder.Options);
    }
}