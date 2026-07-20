using SharedKernel.AspNetCore;
using SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Quests.Api.Filters;
using Quests.Application.Common.Models;
using Quests.Application.Quests.Commands;
using Quests.Application.Quests.Queries;
using Quests.Domain.Entities;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Quests.Api.EndpointDefinitions;

public class QuestEndpointDefinitions : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        var quests = app.MapGroup("api/quests")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization()
            .AddEndpointFilter<UserIdEndpointFilter>();

        quests.MapGet("", GetMyQuests);
        quests.MapGet("{questId}", GetQuestById);
        quests.MapPost("", CreateQuest);
        quests.MapPut("{questId}", UpdateQuest);
        quests.MapPost("{questId}/complete", CompleteQuest);
        quests.MapDelete("{questId}", DeleteQuest);
        quests.MapPost("{questId}/reminders", CreateQuestReminder);
    }

    private async Task<Ok<List<Quest>>> GetMyQuests(
        Mediator.Mediator mediator,
        HttpContext context,
        QuestStatus? status)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetMyQuests(userId, status);
        var result = await mediator.Send(query);

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Ok<Quest>, NotFound<Error>>> GetQuestById(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid questId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetQuestById(userId, questId);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Created<Quest>, BadRequest<Error>>> CreateQuest(
        Mediator.Mediator mediator,
        HttpContext context,
        QuestForCreationDto quest)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CreateQuest(userId, quest);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/quests/{result.ValueOrThrow.Id}", result.ValueOrThrow);
    }

    private async Task<Results<Ok<Quest>, BadRequest<Error>, NotFound<Error>>> UpdateQuest(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid questId,
        QuestForUpdateDto quest)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new UpdateQuest(userId, questId, quest);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Ok<Quest>, NotFound<Error>>> CompleteQuest(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid questId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CompleteQuest(userId, questId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<NoContent, NotFound<Error>>> DeleteQuest(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid questId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new DeleteQuest(userId, questId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.NoContent();
    }

    private async Task<Results<Created<QuestReminder>, BadRequest<Error>, NotFound<Error>>> CreateQuestReminder(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid questId,
        QuestReminderForCreationDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CreateQuestReminder(userId, questId, request.RemindAt, request.TimeZone, request.IsEnabled);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/quests/{questId}/reminders/{result.ValueOrThrow.Id}", result.ValueOrThrow);
    }
}
