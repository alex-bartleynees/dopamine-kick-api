using Ardalis.GuardClauses;
using Common.Abstractions.Results;
using Mediator;
using Payments.Application.Abstractions;
using Payments.Domain.Entities;

namespace Payments.Application.Payments.Commands;

/// <summary>
/// Durably records a received webhook in the inbox before the endpoint acks Stripe with 200. Deduped
/// on the Stripe event id, so a redelivered webhook is a no-op. The actual resync happens later in the
/// inbox poller — this just guarantees the work is persisted and can't be lost on a crash/deploy.
/// </summary>
public record RecordWebhookEvent(string EventReference, string EventType, string CustomerReference) : IRequest<Result>;

public class RecordWebhookEventHandler(
    IPaymentsRepository repository,
    IPaymentsUnitOfWork unitOfWork) : IRequestHandler<RecordWebhookEvent, Result>
{
    public async ValueTask<Result> Handle(RecordWebhookEvent request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (await repository.InboxEventExistsAsync(request.EventReference, cancellationToken))
        {
            return Result.Success();
        }

        await repository.AddInboxMessageAsync(
            new InboxMessage
            {
                Id = Guid.NewGuid(),
                EventReference = request.EventReference,
                EventType = request.EventType,
                CustomerReference = request.CustomerReference
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
