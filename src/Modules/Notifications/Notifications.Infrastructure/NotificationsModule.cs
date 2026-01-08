using Common.Abstractions.Messaging;
using Common.Infrastructure.Interceptors;
using Common.IntegrationEvents.Habits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Handlers;
using Notifications.Infrastructure.BackgroundServices;
using Notifications.Infrastructure.DbContexts;

namespace Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceProvider MigrateNotificationsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("NotificationsDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<NotificationsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<NotificationsContext>());

        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<HabitReminderCreated>, HabitReminderCreatedHandler>();

        // Register background services
        services.AddHostedService<HabitReminderConsumerService>();

        return services;
    }
}
