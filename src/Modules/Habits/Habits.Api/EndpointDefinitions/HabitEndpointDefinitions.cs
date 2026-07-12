using Common.Abstractions;
using Common.Abstractions.Results;
using Habits.Api.Filters;
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
            .RequireAuthorization()
            .AddEndpointFilter<UserIdEndpointFilter>();

        habits.MapGet("", GetMyHabits);
        habits.MapGet("{habitId}", GetHabitById);
        habits.MapPost("", CreateHabit);
        habits.MapPost("bulk", BulkCreateHabits);
        habits.MapPut("{habitId}", UpdateHabit);
        habits.MapDelete("{habitId}", DeleteHabit);
        habits.MapPost("{habitId}/completions", MarkHabitCompleted);
        habits.MapGet("completions", GetMyHabitCompletions);
        habits.MapGet("{habitId}/completions", GetHabitCompletions);
        habits.MapGet("{habitId}/reminders", GetHabitReminders);
        habits.MapPost("{habitId}/reminders", CreateHabitReminder);
        habits.MapPost("reminders/bulk", BulkCreateHabitReminders);
        habits.MapPut("{habitId}/reminders/{reminderId}", UpdateHabitReminder);
        habits.MapDelete("{habitId}/reminders/{reminderId}", DeleteHabitReminder);
    }

    private async Task<Ok<List<Habit>>> GetMyHabits(
        Mediator.Mediator mediator,
        HttpContext context)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetMyHabits(userId);
        var result = await mediator.Send(query);

        return TypedResults.Ok(result.ValueOrThrow);
    }
    
    private async Task<Results<Ok<Habit>, NotFound<Error>>> GetHabitById(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetHabitById(userId, habitId);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Created<Habit>, BadRequest<Error>>> CreateHabit(
        Mediator.Mediator mediator,
        HttpContext context,
        HabitForCreationDto habit)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CreateHabit(userId, habit);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/habits/{result.ValueOrThrow.Id}", result.ValueOrThrow);
    }

    private async Task<Results<Created<List<Habit>>, BadRequest<Error>>> BulkCreateHabits(
        Mediator.Mediator mediator,
        HttpContext context,
        BulkHabitsForCreationDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new BulkCreateHabits(userId, request.Habits);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created("/api/habits", result.ValueOrThrow);
    }

    private async Task<Results<Created<HabitCompletion>, BadRequest<Error>>> MarkHabitCompleted(
        Mediator.Mediator mediator,
        HttpContext context,
        HabitForCompletionDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CreateHabitCompletion(userId, request.HabitId, request.Timezone);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/habits/{request.HabitId}", result.ValueOrThrow);
    }

    private async Task<Results<Created<Guid>, BadRequest<Error>, NotFound<Error>>> CreateHabitReminder(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId,
        HabitReminderForCreationDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new CreateHabitReminder(habitId, userId, request.NotificationTime, request.TimeZone,
            request.PreferredTime, request.IsEnabled);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created($"/api/habits/{habitId}/reminders/{result.ValueOrThrow}", result.ValueOrThrow);
    }

    private async Task<Results<Created<List<Guid>>, BadRequest<Error>>> BulkCreateHabitReminders(
        Mediator.Mediator mediator,
        HttpContext context,
        BulkHabitRemindersForCreationDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new BulkCreateHabitReminders(userId, request.Reminders);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Created("/api/habits/reminders", result.ValueOrThrow);
    }

    private async Task<Results<Ok<Habit>, BadRequest<Error>, NotFound<Error>>> UpdateHabit(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId,
        HabitForUpdateDto habit)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new UpdateHabit(userId, habitId, habit);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<NoContent, NotFound<Error>>> DeleteHabit(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new DeleteHabit(userId, habitId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.NoContent();
    }

    private async Task<Ok<List<HabitReminder>>> GetHabitReminders(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetHabitReminders(userId, habitId);
        var result = await mediator.Send(query);

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Ok<Guid>, BadRequest<Error>, NotFound<Error>>> UpdateHabitReminder(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId,
        Guid reminderId,
        HabitReminderForUpdateDto request)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new UpdateHabitReminder(userId, reminderId, request);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<NoContent, NotFound<Error>>> DeleteHabitReminder(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId,
        Guid reminderId)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var command = new DeleteHabitReminder(userId, reminderId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return TypedResults.NotFound(result.Error);
        }

        return TypedResults.NoContent();
    }

    private async Task<Results<Ok<AllHabitCompletionHistoryDto>, BadRequest<Error>>> GetMyHabitCompletions(
        Mediator.Mediator mediator,
        HttpContext context,
        string timezone,
        int days = 30)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetMyHabitCompletions(userId, days, timezone);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }

    private async Task<Results<Ok<HabitCompletionHistoryDto>, BadRequest<Error>, NotFound<Error>>> GetHabitCompletions(
        Mediator.Mediator mediator,
        HttpContext context,
        Guid habitId,
        string timezone,
        int days = 30)
    {
        var userId = (Guid)context.Items[UserIdEndpointFilter.UserIdKey]!;

        var query = new GetHabitCompletions(userId, habitId, days, timezone);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? TypedResults.NotFound(result.Error)
                : TypedResults.BadRequest(result.Error);
        }

        return TypedResults.Ok(result.ValueOrThrow);
    }
}
