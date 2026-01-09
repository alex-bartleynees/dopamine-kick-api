using System.Text.Json;
using Common.Abstractions.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.DbContexts;
using WebPush;

namespace Notifications.Infrastructure.Services;

public class WebPushService(NotificationsContext context, IOptions<WebPushOptions> options, ILogger<WebPushService> logger) : IWebPushService
{
    private readonly WebPushOptions _webPushOptions = options.Value;
    
    private async Task<Result> SendNotificationAsync(WebPushSubscription subscription, string title, string body, string? icon, object? data,
        CancellationToken ct)
    {
        var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
        var vapidDetails = new VapidDetails(_webPushOptions.Subject, _webPushOptions.PublicKey, _webPushOptions.PrivateKey);
        var webPushClient = new WebPushClient();

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                icon,
                data
            });

            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails, ct);

            // Success - update subscription
            subscription.LastNotificationSentAt = DateTimeOffset.UtcNow;
            subscription.FailureCount = 0;

            return Result.Success();
        }
        catch (WebPushException ex)
        {
            // Handle specific HTTP status codes
            if (ex.StatusCode == System.Net.HttpStatusCode.Gone) // 410 - subscription expired
            {
                logger.LogWarning("Subscription {Id} expired (410 Gone), marking as inactive", subscription.Id);
                subscription.IsActive = false;
                return Result.Failure(new Error(410, "Subscription Expired", "Push subscription has expired"));
            }

            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) // 404 - subscription not found
            {
                logger.LogWarning("Subscription {Id} not found (404), marking as inactive", subscription.Id);
                subscription.IsActive = false;
                return Result.Failure(new Error(404, "Subscription Not Found", "Push subscription not found"));
            }

            // Other errors - increment failure count
            subscription.FailureCount++;

            if (subscription.FailureCount >= 5)
            {
                logger.LogWarning("Subscription {Id} failed {Count} times, marking as inactive",
                    subscription.Id, subscription.FailureCount);
                subscription.IsActive = false;
            }

            logger.LogError(ex, "Failed to send notification to subscription {Id} (Attempt {Count})",
                subscription.Id, subscription.FailureCount);

            return Result.Failure(new Error((int?)ex.StatusCode ?? 500, "WebPush Send Failed", ex.Message));
        }
        catch (Exception ex)
        {
            subscription.FailureCount++;

            if (subscription.FailureCount >= 5)
            {
                logger.LogWarning("Subscription {Id} failed {Count} times, marking as inactive",
                    subscription.Id, subscription.FailureCount);
                subscription.IsActive = false;
            }

            logger.LogError(ex, "Unexpected error sending notification to subscription {Id}", subscription.Id);

            return Result.Failure(new Error(500, "Unexpected Error", ex.Message));
        }
    }

    public async Task SendNotificationToUserAsync(Guid userId, string title, string body, string? icon, object? data,
        CancellationToken ct)
    {
        var subscriptions = await context.WebPushSubscriptions
            .Where(sub => sub.UserId == userId && sub.IsActive)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            logger.LogInformation("No active subscriptions found for user {UserId}", userId);
            return;
        }

        // Send to all subscriptions in parallel
        var tasks = subscriptions.Select(subscription =>
            SendNotificationAsync(subscription, title, body, icon, data, ct));

        var results = await Task.WhenAll(tasks);

        // Save all subscription updates to database
        await context.SaveChangesAsync(ct);

        // Check if at least one succeeded
        var successCount = results.Count(r => r.IsSuccess);

        if (successCount > 0)
        {
            logger.LogInformation("Successfully sent notification to {SuccessCount}/{Total} subscriptions for user {UserId}",
                successCount, results.Length, userId);
        }
        else
        {
            logger.LogWarning("Failed to send notification to all {Total} subscriptions for user {UserId}",
                results.Length, userId);
        }
    }
}