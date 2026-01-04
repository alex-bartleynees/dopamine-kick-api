namespace Habits.Application.Common.Models;

public record BulkHabitsForCreationDto(List<HabitForCreationDto> Habits);