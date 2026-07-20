using Ardalis.GuardClauses;
using SharedKernel.Results;
using Mediator;
using Payments.Application.Abstractions;

namespace Payments.Application.Payments.Commands;

/// <summary>
/// Idempotently ensures a Stripe customer exists for the user and the binding is persisted.
/// Returns the (existing or newly created) Stripe customer id. This is the "eager" binding step.
/// </summary>
public record EnsureCustomer(Guid UserId, string Email) : IRequest<Result<string>>;

public class EnsureCustomerHandler(
    ICustomerService customerService) : IRequestHandler<EnsureCustomer, Result<string>>
{
    public async ValueTask<Result<string>> Handle(EnsureCustomer request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        return await customerService.EnsureCustomerAsync(request.UserId, request.Email, cancellationToken);
    }
}
