using SharedKernel.Results;

namespace Users.Infrastructure.Errors;

/// <summary>
/// External-service (Keycloak) failures surfaced by the identity integration, grouped in one place.
/// These are infrastructure failures rather than domain rule violations.
/// </summary>
public static class KeycloakErrors
{
    public static Error SearchFailed(string detail) =>
        Error.Failure("Keycloak.SearchFailed", $"Failed to search users: {detail}");

    public static Error CreateFailed(string detail) =>
        Error.Failure("Keycloak.CreateFailed", $"Failed to create user: {detail}");

    public static Error TokenRequestFailed(string detail) =>
        Error.Failure("Keycloak.TokenRequestFailed", $"Failed to get Keycloak token: {detail}");

    public static Error TokenParseFailed() =>
        Error.Failure("Keycloak.TokenParseFailed", "Failed to parse token response");
}
