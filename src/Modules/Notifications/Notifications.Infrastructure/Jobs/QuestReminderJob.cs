using System.Text.Json;
using Ardalis.GuardClauses;
using Common.IntegrationEvents.Quests;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Quartz;

namespace Notifications.Infrastructure.Jobs;

public class QuestReminderJob(IWebPushService webPushService, ILogger<QuestReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var messageId = dataMap.GetString("MessageId");
            var userId = dataMap.GetString("UserId");
            var questJson = dataMap.GetString("QuestJson");

            Guard.Against.Null(messageId);
            Guard.Against.Null(questJson);

            var quest = JsonSerializer.Deserialize<QuestReminderCreated>(questJson);
            if (quest == null)
            {
                logger.LogError("Failed to deserialize quest from job data for message {MessageId}", messageId);
                return;
            }

            await webPushService.SendNotificationToUserAsync(
                quest.UserId,
                $"{quest.QuestEmoji} {quest.QuestTitle}",
                $"Reminder: your quest '{quest.QuestTitle}' is due soon!",
                quest.QuestEmoji,
                new { type = "quest_reminder", questId = quest.QuestId, reminderId = quest.ReminderId, questTitle = quest.QuestTitle },
                CancellationToken.None
            );

            logger.LogInformation("Successfully sent quest reminder for '{QuestTitle}' to user {UserId}",
                quest.QuestTitle, userId);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error deserializing quest data in job execution");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing quest reminder job");
            throw;
        }
    }
}
