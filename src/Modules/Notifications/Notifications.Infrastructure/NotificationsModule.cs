using Common.Abstractions.Messaging;
using Common.Infrastructure.Interceptors;
using Common.IntegrationEvents.Habits;
using Habits.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Handlers;
using Notifications.Infrastructure.BackgroundServices;
using Notifications.Infrastructure.DbContexts;
using Notifications.Infrastructure.Services;
using Quartz;
using Quartz.Impl.AdoJobStore;

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

        services.AddQuartz(options =>
        {
            options.UsePersistentStore(c =>
            {
                c.RetryInterval = TimeSpan.FromMinutes(2);
                c.UseProperties = true;
                c.PerformSchemaValidation = true;
                c.UseSystemTextJsonSerializer();

                c.UsePostgres(postgres =>
                {
                    postgres.ConnectionString = cs;
                    postgres.TablePrefix = "quartz.qrtz_";
                    postgres.UseDriverDelegate<PostgreSQLDelegate>();
                });
            });
        });

        services.AddQuartzHostedService(options =>
        {
            // When shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });

        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<HabitReminderCreated>, HabitReminderCreatedHandler>();

        services.AddScoped<IJobScheduler, JobSchedulerService>();
        services.AddScoped<IProcessedMessageService, ProcessedMessageService>();

        // Register background services
        services.AddHostedService<HabitReminderConsumerService>();

        return services;
    }
}
