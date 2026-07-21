using Entitlements.Domain;
using Entitlements.Infrastructure.BackgroundServices;
using Entitlements.Infrastructure.DbContexts;
using Entitlements.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;

namespace Entitlements.Api;

public static class EntitlementsModule
{
    public static IServiceProvider MigrateEntitlementsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntitlementsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddEntitlementsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("EntitlementsDBConnectionString")
            ?? throw new ArgumentNullException(nameof(configuration), "No connection string provided");

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<EntitlementsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<ISubscriptionAccessService, SubscriptionAccessService>();
        services.AddHostedService<EntitlementConsumerService>();

        return services;
    }
}
