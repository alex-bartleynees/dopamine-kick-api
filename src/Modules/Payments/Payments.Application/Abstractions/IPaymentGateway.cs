using Payments.Application.Common.Models;

namespace Payments.Application.Abstractions;

/// <summary>
/// The domain's port onto the payment provider — the anti-corruption layer boundary. Named after the
/// capability it provides, not the vendor behind it; the only implementation
/// (<c>StripePaymentGateway</c>) confines every provider SDK type. All URLs, price ids, trial length
/// and the webhook secret come from configuration inside the implementation, so callers only pass the
/// opaque customer reference.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Creates a provider customer for the user and returns its opaque reference.</summary>
    Task<string> CreateCustomerAsync(Guid userId, string email, CancellationToken ct = default);

    Task<string> CreateCheckoutSessionUrlAsync(string customerReference, CancellationToken ct = default);

    Task<string> CreatePortalSessionUrlAsync(string customerReference, CancellationToken ct = default);

    /// <summary>
    /// Fetches the customer's most recent subscription and translates it to a domain snapshot.
    /// Returns <c>null</c> when the customer has never had a subscription.
    /// </summary>
    Task<SubscriptionSnapshot?> GetLatestSubscriptionAsync(string customerReference, CancellationToken ct = default);

    /// <summary>
    /// Verifies the provider signature and returns the event reference, type and customer reference,
    /// or <c>null</c> if the signature is invalid. The payload is never trusted beyond routing to a resync.
    /// </summary>
    PaymentProviderNotification? ParseWebhookEvent(string payload, string signature);
}
