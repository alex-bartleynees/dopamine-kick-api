using Common.Abstractions;

namespace Habits.Domain.Entities;

public class HabitReminder : IAuditable
{
    public Guid Id { get; set; }
    
    public Guid HabitId { get; set; }
    
    public Guid UserId { get; set; }
    
    public TimeOnly NotificationTime { get; set; }

    public string TimeZone { get; set; } = string.Empty;

    public string PreferredTime { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Habit Habit { get; set; } = null!;
}