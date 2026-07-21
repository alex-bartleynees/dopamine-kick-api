namespace Common.IntegrationEvents;

/// <summary>
/// Routing keys for this product's integration events. Deliberately app-specific and kept out of the
/// shared kernel: routing keys are a bounded-context concern (they travel on each event via
/// <c>[IntegrationEventRoutingKey]</c>), so they live alongside the event contracts here rather than in
/// the reusable messaging infrastructure.
/// </summary>
public static class RoutingKeys
{
    public const string HabitReminderCreatedKey = "habit.reminder.created";
    public const string HabitReminderCancelledKey = "habit.reminder.cancelled";
    public const string QuestReminderCreatedKey = "quest.reminder.created";
    public const string QuestReminderCancelledKey = "quest.reminder.cancelled";
    public const string SubscriptionEntitlementChangedKey = "subscription.entitlement.changed";
}
