using System.Security.Claims;
using Common.Abstractions;
using Common.Abstractions.Extensions;
using Common.Abstractions.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Payments.Application.Common.Models;
using Payments.Application.Payments.Commands;
using Payments.Application.Payments.Queries;
using Payments.Domain.Errors;

namespace Payments.Api.EndpointDefinitions;

public class BillingEndpointDefinitions : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        var billing = app.MapGroup("api/billing").RequireAuthorization();

        billing.MapPost("customer", EnsureCustomer);
        billing.MapPost("checkout", CreateCheckout);
        billing.MapPost("portal", CreatePortal);
        billing.MapPost("sync", Sync);
        billing.MapGet("subscription", GetSubscription);
    }

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> EnsureCustomer(
        Payments.Api.Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        if (user.GetUserId() is not { } userId)
        {
            return PaymentsErrors.MissingUserId.ToProblem();
        }

        var result = await mediator.Send(new EnsureCustomer(userId, GetEmail(user)));
        return result.IsSuccess
            ? TypedResults.Ok(new CustomerResponse(result.ValueOrThrow))
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<CheckoutSessionResponse>, ProblemHttpResult>> CreateCheckout(
        Payments.Api.Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        if (user.GetUserId() is not { } userId)
        {
            return PaymentsErrors.MissingUserId.ToProblem();
        }

        var result = await mediator.Send(new CreateCheckoutSession(userId, GetEmail(user)));
        return result.IsSuccess ? TypedResults.Ok(result.ValueOrThrow) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<PortalSessionResponse>, ProblemHttpResult>> CreatePortal(
        Payments.Api.Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        if (user.GetUserId() is not { } userId)
        {
            return PaymentsErrors.MissingUserId.ToProblem();
        }

        var result = await mediator.Send(new CreatePortalSession(userId));
        return result.IsSuccess ? TypedResults.Ok(result.ValueOrThrow) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok, ProblemHttpResult>> Sync(
        Payments.Api.Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        if (user.GetUserId() is not { } userId)
        {
            return PaymentsErrors.MissingUserId.ToProblem();
        }

        var result = await mediator.Send(new SyncSubscription(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<SubscriptionStateDto>, ProblemHttpResult>> GetSubscription(
        Payments.Api.Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        if (user.GetUserId() is not { } userId)
        {
            return PaymentsErrors.MissingUserId.ToProblem();
        }

        var dto = await mediator.Send(new GetSubscriptionState(userId));
        return TypedResults.Ok(dto);
    }

    private static string GetEmail(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? string.Empty;
}

// The customer reference is an opaque provider id — named after the concept, not the vendor, on the
// wire too. Serializes as camelCase `customerReference`.
public record CustomerResponse(string CustomerReference);

internal static class BillingErrorExtensions
{
    public static ProblemHttpResult ToProblem(this Error error) =>
        TypedResults.Problem(detail: error.Detail, statusCode: error.Status, title: error.Title);
}
