namespace Common.Abstractions.Messaging;

public static class MessagingConstants
{
    public const string ExchangeName = "habits-direct";
    public const string ExchangeType = "direct";
    
    // Routing Keys
    public const string HabitReminderCreatedKey = "habit.reminder.created";
    public const string HabitReminderUpdatedKey = "habit.reminder.updated";
    
    // Queue Names
    public const string HabitRemindersQueue = "habit-reminders";
}