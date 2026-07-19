using Common.Abstractions.Results;

namespace Payments.Application.Abstractions;

/// <summary>
/// Owns the "eager" customer binding: idempotently ensures a provider customer exists for the user
/// and the mapping is persisted, returning the (existing or newly created) opaque customer reference.
/// Extracted from the mediator handler so it can be shared by the ensure-customer endpoint and the
/// checkout flow without one in-process handler dispatching to another.
/// </summary>
public interface ICustomerService
{
    Task<Result<string>> EnsureCustomerAsync(Guid userId, string email, CancellationToken ct = default);
}
