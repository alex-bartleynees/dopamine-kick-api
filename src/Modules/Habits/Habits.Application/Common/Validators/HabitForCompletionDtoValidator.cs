using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class HabitForCompletionDtoValidator : AbstractValidator<HabitForCompletionDto>
{
    public HabitForCompletionDtoValidator()
    {
        RuleFor(x => x.HabitId).NotEmpty();
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .Must(BeAValidTimezone)
            .WithMessage("'{PropertyValue}' is not a valid IANA timezone identifier.");
    }

    private static bool BeAValidTimezone(string timezone)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    }
}
