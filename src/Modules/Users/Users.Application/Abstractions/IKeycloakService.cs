using Common.Abstractions.Results;
using Users.Application.Common.Models;

namespace Users.Application.Abstractions;

public interface IKeycloakService
{
    Task<Result<KeycloakUserResponse>> GetUserByEmailAsync(string email);
    Task<Result<KeycloakUserResponse>> CreateUserAsync(UserForCreationDto user);
}