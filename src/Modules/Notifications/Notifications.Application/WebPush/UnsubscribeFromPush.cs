using Common.Abstractions.Results;
using Mediator;
using Notifications.Application.Abstractions;
using Notifications.Domain.Errors;

namespace Notifications.Application.WebPush;

public record UnsubscribeFromPushCommand(
    Guid SubscriptionId,
    Guid UserId
) : IRequest<Result>;

public class UnsubscribeFromPushCommandHandler(
    INotificationsRepository repository,
    INotificationsUnitOfWork unitOfWork) : IRequestHandler<UnsubscribeFromPushCommand, Result>
{
    public async ValueTask<Result> Handle(UnsubscribeFromPushCommand request, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByIdAsync(request.SubscriptionId, request.UserId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(PushSubscriptionErrors.NotFound());
        }

        repository.Delete(subscription);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
