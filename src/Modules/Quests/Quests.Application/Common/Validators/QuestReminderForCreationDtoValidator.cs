using FluentValidation;
using Quests.Application.Common.Models;

namespace Quests.Application.Common.Validators;

public class QuestReminderForCreationDtoValidator : AbstractValidator<QuestReminderForCreationDto>
{
    public QuestReminderForCreationDtoValidator()
    {
        RuleFor(x => x.RemindAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("'Remind At' must be in the future.");
        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(BeAValidTimezone)
            .WithMessage("'{PropertyValue}' is not a valid IANA timezone identifier.");
    }

    private static bool BeAValidTimezone(string timezone)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    }
}
