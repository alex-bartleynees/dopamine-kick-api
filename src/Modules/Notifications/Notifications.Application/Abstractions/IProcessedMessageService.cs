namespace Notifications.Application.Abstractions;

public interface IProcessedMessageService
{
    Task<bool> IsMessageProcessedAsync(Guid messageId);
    Task MarkAsProcessedAsync(Guid messageId, string messageType);
    Task CleanupOldMessagesAsync(TimeSpan retention);
}