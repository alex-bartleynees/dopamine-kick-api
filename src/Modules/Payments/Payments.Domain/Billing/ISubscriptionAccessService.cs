namespace Payments.Domain.Billing;

/// <summary>
/// Cross-module read of a user's subscription entitlement. Declared here as a shared
/// abstraction (not a module reference) so the Host and other modules can gate premium
/// features without depending on the Payments module directly — mirroring how
/// <c>IMessagePublisher</c> is shared. Implemented by the Payments module.
/// </summary>
public interface ISubscriptionAccessService
{
    /// <summary>
    /// True when the user currently has access to paid features: an <c>active</c> or
    /// <c>trialing</c> subscription, including the grace period after a scheduled
    /// cancellation (still active until the period end).
    /// </summary>
    Task<bool> HasActiveAccessAsync(Guid userId, CancellationToken ct = default);
}
