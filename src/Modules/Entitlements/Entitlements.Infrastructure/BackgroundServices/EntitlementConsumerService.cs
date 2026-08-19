using Common.IntegrationEvents;
using Common.IntegrationEvents.Payments;
using Entitlements.Domain;
using Entitlements.Domain.Entities;
using Entitlements.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Messaging.Abstractions;

namespace Entitlements.Infrastructure.BackgroundServices;

public class EntitlementConsumerService(
    IMessageConsumer consumer,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EntitlementConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Entitlement consumer starting");

        await consumer.Subscribe<SubscriptionEntitlementChanged>(
            queueName: "payments.entitlements",
            routingKey: RoutingKeys.SubscriptionEntitlementChangedKey,
            handler: async @event =>
            {
                if (@event.ProductId != EntitlementProducts.DopamineKick)
                {
                    return;
                }

                using var scope = serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EntitlementsContext>();

                var entitlement = await context.Entitlements
                    .FirstOrDefaultAsync(
                        e => e.ProductId == @event.ProductId && e.UserId == @event.UserId,
                        stoppingToken);

                if (entitlement is null)
                {
                    entitlement = new Entitlement { ProductId = @event.ProductId, UserId = @event.UserId };
                    context.Entitlements.Add(entitlement);
                }

                entitlement.HasAccess = @event.HasAccess;
                entitlement.Status = @event.Status;
                entitlement.CurrentPeriodEnd = @event.CurrentPeriodEnd;

                await context.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "Upserted entitlement for user {UserId} product {ProductId}: HasAccess={HasAccess} Status={Status}",
                    @event.UserId, @event.ProductId, @event.HasAccess, @event.Status);
            },
            exchange: "payments-direct");
    }
}
