using Payments.Domain.Billing;
using SharedKernel.Abstractions;
using Microsoft.AspNetCore.Http;

namespace WebApi.Filters;

/// <summary>
/// Gates premium endpoints on an active entitlement. Lives in the Host so it can depend on the shared
/// <see cref="ISubscriptionAccessService"/> (implemented by the Payments module) without any module
/// referencing another. Apply with <c>.AddEndpointFilter&lt;RequireActiveSubscriptionFilter&gt;()</c>
/// on endpoints that require a subscription.
/// </summary>
public class RequireActiveSubscriptionFilter(ISubscriptionAccessService accessService) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.User.GetUserId();
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var hasAccess = await accessService.HasActiveAccessAsync(userId.Value, context.HttpContext.RequestAborted);
        if (!hasAccess)
        {
            return TypedResults.Problem(
                detail: "An active subscription is required to access this feature.",
                statusCode: StatusCodes.Status402PaymentRequired,
                title: "Payment Required");
        }

        return await next(context);
    }
}
