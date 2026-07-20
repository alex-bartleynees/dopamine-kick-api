using SharedKernel.Abstractions;

namespace Payments.Domain.Entities;

/// <summary>
/// Eager binding between an app <see cref="UserId"/> (Keycloak <c>sub</c>) and the payment provider
/// customer created for them. Created before any Checkout Session exists so Checkout never implicitly
/// creates a second customer for the same user.
/// </summary>
public class CustomerMapping : IAuditable
{
    public Guid UserId { get; set; }

    public string CustomerReference { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public CustomerMapping() { }

    public CustomerMapping(Guid userId, string customerReference)
    {
        UserId = userId;
        CustomerReference = customerReference;
    }
}
