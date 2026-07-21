using SharedKernel.Abstractions;
using SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace Quests.Api.Filters;

public class UserIdEndpointFilter : IEndpointFilter
{
    public const string UserIdKey = "UserId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;
        var userId = user.GetUserId();

        if (userId is null)
        {
            return TypedResults.BadRequest(Error.Validation("Auth.MissingUserId", "User ID not found in claims"));
        }

        context.HttpContext.Items[UserIdKey] = userId.Value;
        return await next(context);
    }
}
