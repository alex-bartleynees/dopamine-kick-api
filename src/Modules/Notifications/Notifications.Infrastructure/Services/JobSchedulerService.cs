using System.Text.Json;
using Common.IntegrationEvents.Habits;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Jobs;
using Quartz;

namespace Notifications.Infrastructure.Services;

public class JobSchedulerService(
    ISchedulerFactory schedulerFactory,
    ILogger<JobSchedulerService> logger) : IJobScheduler
{
    public async Task ScheduleHabitReminderAsync(HabitReminderCreated message)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var jobKey = new JobKey($"habit-reminder-{message.ReminderId}-user-{message.UserId}");
        var triggerKey = new TriggerKey($"habit-trigger-{message.ReminderId}-user-{message.UserId}");

        // Delete existing job if it exists (handles updates to reminder times)
        if (await scheduler.CheckExists(jobKey))
        {
            logger.LogInformation("Job {JobKey} already exists, deleting before rescheduling", jobKey);
            await scheduler.DeleteJob(jobKey);
        }

        var habitJson = JsonSerializer.Serialize(message);

        var jobDetail = JobBuilder.Create<HabitReminderJob>()
            .WithIdentity(jobKey)
            .UsingJobData("MessageId", message.MessageId.ToString())
            .UsingJobData("UserId", message.UserId.ToString())
            .UsingJobData("HabitJson", habitJson)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithSchedule(CronScheduleBuilder
                .DailyAtHourAndMinute(message.NotificationTime.Hour, message.NotificationTime.Minute)
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(message.TimeZone)))
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        logger.LogInformation(
            "Scheduled habit reminder job {JobKey} for daily execution at {NotificationTime} ({TimeZone})",
            jobKey, message.NotificationTime, message.TimeZone);
    }
}