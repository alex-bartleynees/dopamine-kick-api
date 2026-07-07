using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class BulkHabitReminderItemDtoValidator : AbstractValidator<BulkHabitReminderItemDto>
{
    public BulkHabitReminderItemDtoValidator()
    {
        RuleFor(x => x.HabitId).NotEmpty();
        RuleFor(x => x.NotificationTime).NotEmpty();
        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(BeAValidTimezone)
            .WithMessage("'{PropertyValue}' is not a valid IANA timezone identifier.");
        RuleFor(x => x.PreferredTime).NotEmpty();
        RuleFor(x => x.IsEnabled).NotNull();
    }

    private static bool BeAValidTimezone(string timezone)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    }
}
