using FluentValidation;
using Quests.Application.Common.Models;

namespace Quests.Application.Common.Validators;

public class QuestForUpdateDtoValidator : AbstractValidator<QuestForUpdateDto>
{
    public QuestForUpdateDtoValidator()
    {
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DueAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("'Due At' must be in the future.");
    }
}
