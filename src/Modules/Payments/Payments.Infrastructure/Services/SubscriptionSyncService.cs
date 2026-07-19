using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;
using Payments.Domain.Billing;
using Payments.Domain.Entities;

namespace Payments.Infrastructure.Services;

/// <inheritdoc cref="ISubscriptionSyncService"/>
public class SubscriptionSyncService(
    IPaymentsRepository repository,
    IPaymentGateway paymentGateway,
    IPaymentsUnitOfWork unitOfWork,
    ILogger<SubscriptionSyncService> logger) : ISubscriptionSyncService
{
    public async Task SyncAsync(string customerReference, CancellationToken ct = default)
    {
        var snapshot = await paymentGateway.GetLatestSubscriptionAsync(customerReference, ct);

        var state = await repository.GetSubscriptionStateAsync(customerReference, ct);
        if (state is null)
        {
            state = new SubscriptionState { CustomerReference = customerReference };
            await repository.AddSubscriptionStateAsync(state, ct);
        }

        // Full overwrite from the provider's current truth — never a partial patch from a webhook payload.
        if (snapshot is null)
        {
            state.SubscriptionReference = null;
            state.Status = SubscriptionStatus.None;
            state.PriceId = null;
            state.CurrentPeriodStart = null;
            state.CurrentPeriodEnd = null;
            state.CancelAtPeriodEnd = false;
            state.PaymentMethodBrand = null;
            state.PaymentMethodLast4 = null;
        }
        else
        {
            state.SubscriptionReference = snapshot.SubscriptionReference;
            state.Status = snapshot.Status;
            state.PriceId = snapshot.PriceId;
            state.CurrentPeriodStart = snapshot.CurrentPeriodStart;
            state.CurrentPeriodEnd = snapshot.CurrentPeriodEnd;
            state.CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd;
            state.PaymentMethodBrand = snapshot.PaymentMethodBrand;
            state.PaymentMethodLast4 = snapshot.PaymentMethodLast4;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Synced subscription state for customer {CustomerReference}: status={Status}",
            customerReference, state.Status);
    }
}
