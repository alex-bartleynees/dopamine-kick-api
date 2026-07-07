using FluentValidation;
using Habits.Application.Common.Models;

namespace Habits.Application.Common.Validators;

public class HabitForUpdateDtoValidator : AbstractValidator<HabitForUpdateDto>
{
    public HabitForUpdateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Target).NotEmpty().MaximumLength(200);
    }
}
