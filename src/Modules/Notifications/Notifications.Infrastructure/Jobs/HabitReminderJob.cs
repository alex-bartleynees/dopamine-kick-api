using System.Text.Json;
using Ardalis.GuardClauses;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Notifications.Infrastructure.Jobs;

public class HabitReminderJob(ILogger<HabitReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var messageId = dataMap.GetString("MessageId");
            var userId = dataMap.GetString("UserId");
            var habitJson = dataMap.GetString("HabitJson");

            Guard.Against.Null(messageId);
            Guard.Against.Null(habitJson);

            var habit = JsonSerializer.Deserialize<HabitReminderCreated>(habitJson);
            if (habit == null)
            {
                logger.LogError("Failed to deserialize habit from job data for message {MessageId}", messageId);
                return;
            }

            logger.LogInformation($"{habit.HabitEmoji} Time to complete your '{habit.HabitName}' habit!");

            // TODO Send web push notification
            // await webPushService.SendNotification()

            logger.LogInformation("Successfully sent habit reminder for '{HabitName}' to user {UserId}",
                habit.HabitName, userId);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error deserializing habit data in job execution");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing habit reminder job");
            throw;
        }
    }
}