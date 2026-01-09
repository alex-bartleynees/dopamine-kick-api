using Habits.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.DbContexts;

namespace Notifications.Infrastructure.Services;

public class ProcessedMessageService(NotificationsContext context, ILogger<ProcessedMessageService> logger) : IProcessedMessageService
{
    public async Task<bool> IsMessageProcessedAsync(Guid messageId)
    {
        return await context.ProcessedMessages.AnyAsync(pm => pm.MessageId == messageId);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, string messageType)
    {
        var processedMessage = new ProcessedMessage
        {
            MessageId = messageId,
            MessageType = messageType
        };

        context.ProcessedMessages.Add(processedMessage);
        await context.SaveChangesAsync();
        
        logger.LogDebug("Marked message {MessageId} as processed", messageId);
    }

    public async Task CleanupOldMessagesAsync(TimeSpan retention)
    {
        var cutoffDate = DateTime.UtcNow - retention;
        
        var oldMessages = context.ProcessedMessages
            .Where(pm => pm.CreatedAt < cutoffDate);

        context.ProcessedMessages.RemoveRange(oldMessages);
        var deletedCount = await context.SaveChangesAsync();
        
        logger.LogInformation("Cleaned up {Count} old processed messages", deletedCount);
    }
}