using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class HabitReminderForCreationDtoValidator : AbstractValidator<HabitReminderForCreationDto>
{
    public HabitReminderForCreationDtoValidator()
    {
        RuleFor(x => x.HabitId).NotEmpty();
        RuleFor(x => x.NotificationTime).NotEmpty();
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .Must(BeAValidTimezone)
            .WithMessage("'{PropertyValue}' is not a valid IANA timezone identifier.");
        RuleFor(x => x.PreferredTime).NotEmpty();
        RuleFor(x => x.isEnabled).NotNull();
    } 

    private static bool BeAValidTimezone(string timezone)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    } 
}