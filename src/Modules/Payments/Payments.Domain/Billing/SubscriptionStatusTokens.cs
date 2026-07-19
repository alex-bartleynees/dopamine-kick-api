namespace Payments.Domain.Billing;

/// <summary>
/// The single source of truth mapping <see cref="SubscriptionStatus"/> to and from the stable
/// lower-case tokens (e.g. <c>past_due</c>) used on the wire and in storage. Shared by the payment
/// gateway adapter (provider status → domain), the EF value converter (domain ↔ column) and JSON
/// serialization, so all three stay in lock-step and the published frontend contract never drifts.
/// </summary>
public static class SubscriptionStatusTokens
{
    private static readonly IReadOnlyDictionary<SubscriptionStatus, string> ToTokenMap =
        new Dictionary<SubscriptionStatus, string>
        {
            [SubscriptionStatus.Unknown] = "unknown",
            [SubscriptionStatus.None] = "none",
            [SubscriptionStatus.Trialing] = "trialing",
            [SubscriptionStatus.Active] = "active",
            [SubscriptionStatus.PastDue] = "past_due",
            [SubscriptionStatus.Canceled] = "canceled",
            [SubscriptionStatus.Unpaid] = "unpaid",
            [SubscriptionStatus.Incomplete] = "incomplete",
            [SubscriptionStatus.IncompleteExpired] = "incomplete_expired",
            [SubscriptionStatus.Paused] = "paused"
        };

    private static readonly IReadOnlyDictionary<string, SubscriptionStatus> FromTokenMap =
        ToTokenMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    // active/trialing clearly grant access. cancel-at-period-end keeps Status=active until the period
    // ends (the provider flips it to canceled itself), so the grace window needs no extra check.
    // past_due is the intentional soft-warning choice: don't hard-lock on a single failed renewal.
    private static readonly HashSet<SubscriptionStatus> AccessGranting =
    [
        SubscriptionStatus.Active,
        SubscriptionStatus.Trialing,
        SubscriptionStatus.PastDue
    ];

    /// <summary>The stable storage/wire token for a status (falls back to <c>unknown</c>).</summary>
    public static string ToToken(this SubscriptionStatus status) =>
        ToTokenMap.TryGetValue(status, out var token) ? token : "unknown";

    /// <summary>Parses a stored/wire token back to the enum (unrecognised → <see cref="SubscriptionStatus.Unknown"/>).</summary>
    public static SubscriptionStatus FromToken(string token) =>
        FromTokenMap.TryGetValue(token, out var status) ? status : SubscriptionStatus.Unknown;

    /// <summary>Translates a raw provider status string into the domain enum. This is the ACL's
    /// inbound status translation; an unrecognised value maps to <see cref="SubscriptionStatus.Unknown"/>.</summary>
    public static SubscriptionStatus FromProviderStatus(string? providerStatus) =>
        string.IsNullOrEmpty(providerStatus) ? SubscriptionStatus.Unknown : FromToken(providerStatus);

    /// <summary>The single access rule: which statuses entitle the user to premium features.</summary>
    public static bool GrantsAccess(this SubscriptionStatus status) => AccessGranting.Contains(status);
}
