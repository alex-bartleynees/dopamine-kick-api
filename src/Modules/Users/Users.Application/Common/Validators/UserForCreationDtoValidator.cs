using FluentValidation;
using Users.Application.Common.Models;

namespace Users.Application.Common.Validators;

public class UserForCreationDtoValidator : AbstractValidator<UserForCreationDto>
{
    public UserForCreationDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name must be provided");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email must be provided").EmailAddress()
            .WithMessage("Email must be a valid email address");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password must be provided");
    }
}