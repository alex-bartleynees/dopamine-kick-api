using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Entitlements.Infrastructure.DbContexts;

public class EntitlementsContextFactory : IDesignTimeDbContextFactory<EntitlementsContext>
{
    public EntitlementsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<EntitlementsContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("EntitlementsDBConnectionString")
            ?? throw new InvalidOperationException("Connection string not found");

        var optionsBuilder = new DbContextOptionsBuilder<EntitlementsContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EntitlementsContext(optionsBuilder.Options);
    }
}
