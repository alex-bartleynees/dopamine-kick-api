namespace Notifications.Application.Common.Models;

public record SubscribeToPushRequest(
    string Endpoint,
    string P256dh,
    string Auth
 );