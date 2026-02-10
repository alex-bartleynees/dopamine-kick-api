using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class BulkHabitsForCreationDtoValidator : AbstractValidator<BulkHabitsForCreationDto>
{
    public BulkHabitsForCreationDtoValidator(HabitForCreationDtoValidator habitValidator)
    {
        RuleFor(x => x.Habits).Must(list => list.Count >= 1).WithMessage("Please provide at least 1 habits");
        RuleForEach(x => x.Habits).SetValidator(habitValidator);
    }
}