using SharedKernel.Results;

namespace Users.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.User"/> can produce, in one place.
/// </summary>
public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"User with id: {userId} was not found");

    public static Error NotFoundByEmail(string email) =>
        Error.NotFound("Users.NotFoundByEmail", $"User with email {email} not found in Keycloak");

    public static Error EmailAlreadyExists(string email) =>
        Error.Conflict("Users.EmailAlreadyExists", $"User with email {email} already exists");
}
