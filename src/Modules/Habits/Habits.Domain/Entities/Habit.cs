using SharedKernel.Abstractions;

namespace Habits.Domain.Entities;

public class Habit : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Emoji { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    public DateOnly? LastCompletedDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}