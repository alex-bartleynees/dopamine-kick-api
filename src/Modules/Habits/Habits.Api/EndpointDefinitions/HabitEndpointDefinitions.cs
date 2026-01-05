using System.Security.Claims;
using Common.Abstractions;
using Common.Abstractions.Extensions;
using Common.Abstractions.Results;
using Habits.Application.Common.Models;
using Habits.Application.Habits.Commands;
using Habits.Application.Habits.Queries;
using Habits.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Habits.Api.EndpointDefinitions;

public class HabitEndpointDefinitions : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        var habits = app.MapGroup("api/habits")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();

        habits.MapGet("", GetMyHabits);
        habits.MapGet("{habitId}", GetHabitById);
        habits.MapPost("", CreateHabit);
        habits.MapPost("bulk", BulkCreateHabits);
        habits.MapPost("{habitId}/completions", MarkHabitCompleted);
    }

    private async Task<Results<Ok<List<Habit>>, BadRequest<Error>>> GetMyHabits(
        Habits.Api.Mediator.Mediator mediator,
        ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.BadRequest(new Error(400, "BadRequest", "User ID not found in claims"));
        }

        var query = new GetMyHabits(userId.Value);
        var result = await mediator.Send(query);

        return TypedResults.Ok(result.ValueOrThrow);
    }
    
    private async Task<Results<Ok<Habit>, NotFound<Error>, BadRequest<Error>>> GetHabitById(
        Habits.Api.Mediator.Mediator mediator,
        ClaimsPrincipal user,
        Guid habitId)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.BadRequest(new Error(400, "BadRequest", "User ID not found in claims"));
        }

        var query = new GetHabitById(userId.Value, habitId);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Created<Habit>, BadRequest<Error>>> CreateHabit(
        Habits.Api.Mediator.Mediator mediator,
        ClaimsPrincipal user,
        HabitForCreationDto habit)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.BadRequest(new Error(400, "BadRequest", "User ID not found in claims"));
        }

        var command = new CreateHabit(userId.Value, habit);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/habits/{result.ValueOrThrow.Id}", result.ValueOrThrow);
    }

    private async Task<Results<Created<List<Habit>>, BadRequest<Error>>> BulkCreateHabits(
        Habits.Api.Mediator.Mediator mediator,
        ClaimsPrincipal user,
        BulkHabitsForCreationDto request)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.BadRequest(new Error(400, "BadRequest", "User ID not found in claims"));
        }

        var command = new BulkCreateHabits(userId.Value, request.Habits);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created("/api/habits", result.ValueOrThrow);
    }

    private async Task<Results<Created<HabitCompletion>, BadRequest<Error>>> MarkHabitCompleted(
        Habits.Api.Mediator.Mediator mediator,
        ClaimsPrincipal user,
        HabitForCompletionDto request
    )
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.BadRequest(new Error(400, "BadRequest", "User ID not found in claims"));
        }

        var command = new CreateHabitCompletion(userId.Value, request.HabitId, request.Timezone);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/habits/{request.HabitId}", result.ValueOrThrow);
    }
}
