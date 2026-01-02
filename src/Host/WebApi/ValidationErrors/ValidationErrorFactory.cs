using System.Net;
using FluentValidation.Results;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;

namespace WebApi.ValidationErrors;

public class ValidationErrorFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, ValidationResult validationResult)
    {
        var validationProblemErrors = validationResult.ToValidationProblemErrors();

        return Results.ValidationProblem(validationProblemErrors, "There was a validation error", null,
            (int)HttpStatusCode.BadRequest, "Validation Error");
    }
}