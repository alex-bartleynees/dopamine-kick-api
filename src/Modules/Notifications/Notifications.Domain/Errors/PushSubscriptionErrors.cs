using SharedKernel.Results;

namespace Notifications.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.WebPushSubscription"/> can produce, in one place.
/// </summary>
public static class PushSubscriptionErrors
{
    public static Error AlreadyExists() =>
        Error.Conflict("PushSubscriptions.AlreadyExists", "Subscription already exists");

    public static Error NotFound() =>
        Error.NotFound("PushSubscriptions.NotFound", "Subscription not found");

    public static Error Expired() =>
        Error.Gone("PushSubscriptions.Expired", "Push subscription has expired");

    public static Error NotFoundAtProvider() =>
        Error.NotFound("PushSubscriptions.NotFoundAtProvider", "Push subscription not found");

    public static Error SendFailed(string detail) =>
        Error.Failure("PushSubscriptions.SendFailed", detail);

    public static Error Unexpected(string detail) =>
        Error.Failure("PushSubscriptions.Unexpected", detail);
}
