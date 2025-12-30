namespace Users.Application.Common.Models;

public record KeycloakUserResponse(
    string Id,
    string Email,
    string Username,
    string FirstName,
    string LastName
);