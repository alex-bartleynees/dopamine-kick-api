using System.Text.Json;
using Common.IntegrationEvents.Habits;
using Common.IntegrationEvents.Quests;
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

        var (jobKey, triggerKey) = HabitReminderKeys(message.ReminderId, message.UserId);

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

    public async Task CancelHabitReminderAsync(Guid reminderId, Guid userId)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var (jobKey, _) = HabitReminderKeys(reminderId, userId);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
            logger.LogInformation("Cancelled habit reminder job {JobKey}", jobKey);
        }
        else
        {
            logger.LogInformation("Habit reminder job {JobKey} not found, nothing to cancel", jobKey);
        }
    }

    public async Task ScheduleQuestReminderAsync(QuestReminderCreated message)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var (jobKey, triggerKey) = QuestReminderKeys(message.ReminderId, message.UserId);

        // Delete existing job if it exists (handles re-delivery of the same reminder)
        if (await scheduler.CheckExists(jobKey))
        {
            logger.LogInformation("Job {JobKey} already exists, deleting before rescheduling", jobKey);
            await scheduler.DeleteJob(jobKey);
        }

        var questJson = JsonSerializer.Serialize(message);

        var jobDetail = JobBuilder.Create<QuestReminderJob>()
            .WithIdentity(jobKey)
            .UsingJobData("MessageId", message.MessageId.ToString())
            .UsingJobData("UserId", message.UserId.ToString())
            .UsingJobData("QuestJson", questJson)
            .Build();

        // One-off trigger: fire once at the absolute RemindAt time, no repeat.
        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .StartAt(message.RemindAt)
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        logger.LogInformation(
            "Scheduled quest reminder job {JobKey} for one-off execution at {RemindAt}",
            jobKey, message.RemindAt);
    }

    public async Task CancelQuestReminderAsync(Guid reminderId, Guid userId)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var (jobKey, _) = QuestReminderKeys(reminderId, userId);

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
            logger.LogInformation("Cancelled quest reminder job {JobKey}", jobKey);
        }
        else
        {
            logger.LogInformation("Quest reminder job {JobKey} not found, nothing to cancel", jobKey);
        }
    }

    private static (JobKey JobKey, TriggerKey TriggerKey) HabitReminderKeys(Guid reminderId, Guid userId)
    {
        var jobKey = new JobKey($"habit-reminder-{reminderId}-user-{userId}");
        var triggerKey = new TriggerKey($"habit-trigger-{reminderId}-user-{userId}");
        return (jobKey, triggerKey);
    }

    private static (JobKey JobKey, TriggerKey TriggerKey) QuestReminderKeys(Guid reminderId, Guid userId)
    {
        var jobKey = new JobKey($"quest-reminder-{reminderId}-user-{userId}");
        var triggerKey = new TriggerKey($"quest-trigger-{reminderId}-user-{userId}");
        return (jobKey, triggerKey);
    }
}