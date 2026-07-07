namespace Common.Abstractions.Messaging;

public static class MessagingConstants
{
    public const string ExchangeName = "habits-direct";
    public const string ExchangeType = "direct";
    public const string DeadLetterExchangeName = "habits-dlx";
    public const string DeadLetterQueueSuffix = ".dlq";

    // Routing Keys
    public const string HabitReminderCreatedKey = "habit.reminder.created";
    public const string HabitReminderCancelledKey = "habit.reminder.cancelled";
    public const string QuestReminderCreatedKey = "quest.reminder.created";
    public const string QuestReminderCancelledKey = "quest.reminder.cancelled";

    // Queue Names
    public const string HabitRemindersQueue = "habit-reminders";
    public const string HabitRemindersCancelledQueue = "habit-reminders-cancelled";
    public const string QuestRemindersQueue = "quest-reminders";
}