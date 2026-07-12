using Common.Abstractions.Results;
using Mediator;
using Notifications.Application.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Domain.Errors;

namespace Notifications.Application.WebPush;

public record SubscribeToPushCommand(
    Guid UserId,
    string Endpoint,
    string P256dh,
    string Auth
) : IRequest<Result>;

public class SubscribeToPushCommandHandler(
    INotificationsRepository repository,
    INotificationsUnitOfWork unitOfWork) : IRequestHandler<SubscribeToPushCommand, Result>
{
    public async ValueTask<Result> Handle(SubscribeToPushCommand request, CancellationToken cancellationToken)
    {
        var existingSubscription =
            await repository.GetByUserIdAndEndpointAsync(request.UserId, request.Endpoint, cancellationToken);

        if (existingSubscription is not null)
        {
            return Result.Failure(PushSubscriptionErrors.AlreadyExists());
        }

        var subscription = new WebPushSubscription
        {
            UserId = request.UserId,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth
        };

        repository.CreateAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}