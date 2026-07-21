namespace Entitlements.Domain;

/// <summary>
/// Cross-module read of a user's subscription entitlement. Declared here so the Host's
/// <c>RequireActiveSubscriptionFilter</c> can gate features without depending on any specific module.
/// Implemented by the Entitlements module reading its local gateway-sourced read-model.
/// </summary>
public interface ISubscriptionAccessService
{
    Task<bool> HasActiveAccessAsync(Guid userId, CancellationToken ct = default);
}
