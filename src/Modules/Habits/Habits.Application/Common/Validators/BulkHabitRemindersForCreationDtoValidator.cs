using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class BulkHabitRemindersForCreationDtoValidator : AbstractValidator<BulkHabitRemindersForCreationDto>
{
    public BulkHabitRemindersForCreationDtoValidator(HabitReminderForCreationDtoValidator reminderValidator)
    {
        RuleFor(x => x.Reminders).NotEmpty().WithMessage("Please provide at least one reminder");
        RuleForEach(x => x.Reminders).SetValidator(reminderValidator);
    }
}
