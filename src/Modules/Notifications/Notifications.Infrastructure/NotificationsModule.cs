using Common.Abstractions.Messaging;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Handlers;
using Notifications.Infrastructure.BackgroundServices;

namespace Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<HabitReminderCreated>, HabitReminderCreatedHandler>();

        // Register background services
        services.AddHostedService<HabitReminderConsumerService>();

        return services;
    }
}
