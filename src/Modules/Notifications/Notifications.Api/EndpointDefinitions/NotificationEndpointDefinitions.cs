using System.Security.Claims;
using SharedKernel.AspNetCore;
using SharedKernel.Abstractions;
using SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Notifications.Application.Common.Models;
using Notifications.Application.WebPush;
using Notifications.Infrastructure.Services;

namespace Notifications.Api.EndpointDefinitions;

public class NotificationEndpointDefinitions : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        var notifications = app.MapGroup("api/notifications");

        notifications.MapGet("vapid-public-key",
                (IOptions<WebPushOptions> options) => new { publicKey = options.Value.PublicKey })
            .AllowAnonymous();

        notifications.MapPost("/subscriptions", SubscribeToPush).RequireAuthorization().DisableAntiforgery();
        notifications.MapDelete("subscriptions/{id:guid}", UnsubscribeFromPush).RequireAuthorization();
    }

    private async Task<Results<NoContent, BadRequest<Error>, Conflict<Error>>> SubscribeToPush(
        Mediator.Mediator mediator, ClaimsPrincipal user, SubscribeToPushRequest request)
    {
        var userId = user.GetUserId();

        if (userId is null)
        {
            return TypedResults.BadRequest(Error.Validation("Auth.MissingUserId", "User ID not found in claims"));
        }

        var result = await mediator.Send(
            new SubscribeToPushCommand(userId.Value, request.Endpoint, request.P256dh, request.Auth));

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.Conflict
                ? TypedResults.Conflict(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.NoContent();
    }

    private async Task<Results<NoContent, BadRequest<Error>, NotFound<Error>>> UnsubscribeFromPush(
        Guid id, Mediator.Mediator mediator, ClaimsPrincipal user)
    {
        var userId = user.GetUserId();

        if (userId is null)
        {
            return TypedResults.BadRequest(Error.Validation("Auth.MissingUserId", "User ID not found in claims"));
        }

        var result = await mediator.Send(new UnsubscribeFromPushCommand(id, userId.Value));

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.NoContent();
    }
}