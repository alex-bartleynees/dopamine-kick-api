namespace Users.Application.Common.Models;

public record UserForCreationDto(
    string Email,
    string Username,
    string Name,
    string Password,
    string Image
);