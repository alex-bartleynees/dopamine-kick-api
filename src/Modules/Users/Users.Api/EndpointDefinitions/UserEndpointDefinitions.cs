using Common.Abstractions;
using Common.Abstractions.Results;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Users.Application.Common.Models;
using Users.Application.Users.Commands;
using Users.Application.Users.Queries;
using Users.Domain.Entities;

namespace Users.Api.EndpointDefinitions;

public class UserEndpointDefinitions : IEndpointDefinition
{
   public void RegisterEndpoints(WebApplication app)
   {
      var users = app.MapGroup("api/users");
      
      users.MapGet("{userId}", GetUserById).RequireAuthorization();
      users.MapPost("", CreateUser); 
   }

   private async Task<Results<Ok<User>, NotFound<Error>>> GetUserById(IMediator mediator, Guid userId)
   {
      var query = new GetUserById(userId);
      var result = await mediator.Send(query);
      return result.IsSuccess? TypedResults.Ok(result.ValueOrThrow) : TypedResults.NotFound(result.Error);
   }
   
   private async Task<Results<Created<User>, Conflict<Error>, BadRequest<Error>>> CreateUser(
      IMediator mediator,
      UserForCreationDto user)
   {
      var command = new CreateUser(user);
      var result = await mediator.Send(command);

      if (result.IsFailure)
      {
         if (result.Error.Status == 409)
         {
            return TypedResults.Conflict(result.Error);
         }
         return TypedResults.BadRequest(result.Error);
      }

      return TypedResults.Created($"/api/users/{result.ValueOrThrow.Id}", result.ValueOrThrow);
   }
}