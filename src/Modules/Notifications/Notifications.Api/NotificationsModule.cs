using Common.Abstractions.Messaging;
using Common.Infrastructure.Interceptors;
using Common.Infrastructure.Messaging;
using Common.IntegrationEvents.Habits;
using Common.IntegrationEvents.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Handlers;
using Notifications.Infrastructure.BackgroundServices;
using Notifications.Infrastructure.DbContexts;
using Notifications.Infrastructure.Repositories;
using Notifications.Infrastructure.Services;
using Quartz;
using Quartz.Impl.AdoJobStore;

namespace Notifications.Api;

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
        services.AddScoped<INotificationsRepository, NotificationsRepository>();

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
        services.AddScoped<IIntegrationEventHandler<HabitReminderCancelled>, HabitReminderCancelledHandler>();
        services.AddScoped<IIntegrationEventHandler<QuestReminderCreated>, QuestReminderCreatedHandler>();
        services.AddScoped<IIntegrationEventHandler<QuestReminderCancelled>, QuestReminderCancelledHandler>();

        services.AddScoped<IJobScheduler, JobSchedulerService>();
        services.AddScoped<IProcessedMessageService, ProcessedMessageService>();
        services.AddScoped<IWebPushService, WebPushService>();
        services.Configure<WebPushOptions>(
            configuration.GetSection(WebPushOptions.SectionName));
        
        services.AddMediator(options =>
        {
            options.Namespace = "Notifications.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.GenerateTypesAsInternal = true;
        });

        // Register background services
        services.AddHostedService<HabitReminderConsumerService>();
        services.AddHostedService<QuestReminderConsumerService>();
        
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        return services;
    }
}
