using Common.Infrastructure.Interceptors;
using Habits.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Habits.Api;

public static class HabitsModule
{
    public static IServiceProvider MigrateHabitsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HabitsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddHabitsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("HabitsDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<HabitsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));
        
        services.AddMediator(options =>
        {
            options.Namespace = "Habits.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        
        return services;
    }
}