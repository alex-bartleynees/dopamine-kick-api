namespace Payments.Application.Abstractions;

/// <summary>
/// The single source-of-truth writer for <c>SubscriptionState</c>. Re-fetches the full
/// subscription from the payment provider and overwrites the local row. Idempotent by construction,
/// so it can be called from the /success redirect and from the webhook handler in any order, any
/// number of times, without producing split-brain state.
/// </summary>
public interface ISubscriptionSyncService
{
    Task SyncAsync(string customerReference, CancellationToken ct = default);
}
