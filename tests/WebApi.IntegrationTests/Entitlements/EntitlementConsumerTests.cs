using System.Text.Json;
using Common.IntegrationEvents;
using Common.IntegrationEvents.Payments;
using Entitlements.Domain.Entities;
using Entitlements.Infrastructure.DbContexts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace WebApi.IntegrationTests.Entitlements;

/// <summary>
/// Publishes <see cref="SubscriptionEntitlementChanged"/> to the real RabbitMQ container and asserts
/// that <c>EntitlementConsumerService</c> upserts the row into the Entitlements table.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class EntitlementConsumerTests(ApiTestFixture fixture)
{
    private const string ProductId = EntitlementProducts.DopamineKick;

    [Fact]
    public async Task Receiving_event_with_HasAccess_true_creates_entitlement_row()
    {
        var userId = Guid.NewGuid();

        await PublishAsync(userId, ProductId, hasAccess: true, status: "active");

        var row = await WaitForEntitlementAsync(
            userId, ProductId,
            e => e.Status == "active",
            TimeSpan.FromSeconds(15));

        row.Should().NotBeNull("consumer should have upserted the Entitlement row");
        row!.HasAccess.Should().BeTrue();
        row.Status.Should().Be("active");
    }

    [Fact]
    public async Task Second_event_for_same_user_updates_existing_row()
    {
        var userId = Guid.NewGuid();

        await PublishAsync(userId, ProductId, hasAccess: true, status: "trialing");
        await WaitForEntitlementAsync(userId, ProductId, e => e.Status == "trialing", TimeSpan.FromSeconds(15));

        await PublishAsync(userId, ProductId, hasAccess: false, status: "canceled");

        var row = await WaitForEntitlementAsync(
            userId, ProductId,
            e => e.Status == "canceled",
            TimeSpan.FromSeconds(15));

        row.Should().NotBeNull("consumer should have updated the Entitlement row on the second event");
        row!.HasAccess.Should().BeFalse();
        row.Status.Should().Be("canceled");

        // Confirm there is still only one row (upsert, not insert).
        await fixture.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<EntitlementsContext>();
            var count = await db.Entitlements
                .AsNoTracking()
                .CountAsync(e => e.UserId == userId && e.ProductId == ProductId);
            count.Should().Be(1);
        });
    }

    [Fact]
    public async Task Event_for_another_product_is_ignored()
    {
        var userId = Guid.NewGuid();

        await PublishAsync(userId, "coffee_journal", hasAccess: true, status: "active");

        var row = await WaitForEntitlementAsync(
            userId,
            "coffee_journal",
            _ => true,
            TimeSpan.FromSeconds(2));

        row.Should().BeNull("the consumer should only project dopamine_kick entitlements");
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task PublishAsync(Guid userId, string productId, bool hasAccess, string status)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName")!,
            Port = int.Parse(Environment.GetEnvironmentVariable("RabbitMQ__Port")!),
            UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName")!,
            Password = Environment.GetEnvironmentVariable("RabbitMQ__Password")!,
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(false, false));

        // Declare idempotently — the consumer already did this on startup, but ensure it's present.
        await channel.ExchangeDeclareAsync(
            exchange: "payments-direct",
            type: "direct",
            durable: true,
            autoDelete: false);

        var @event = new SubscriptionEntitlementChanged(
            MessageId: Guid.NewGuid(),
            ProductId: productId,
            UserId: userId,
            Status: status,
            HasAccess: hasAccess,
            CurrentPeriodEnd: DateTimeOffset.UtcNow.AddDays(30),
            CancelAtPeriodEnd: false);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
        };

        await channel.BasicPublishAsync(
            exchange: "payments-direct",
            routingKey: RoutingKeys.SubscriptionEntitlementChangedKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private async Task<Entitlement?> WaitForEntitlementAsync(
        Guid userId, string productId,
        Func<Entitlement, bool> predicate,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            Entitlement? row = null;
            await fixture.WithScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<EntitlementsContext>();
                var candidate = await db.Entitlements
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.ProductId == productId);
                if (candidate is not null && predicate(candidate))
                    row = candidate;
            });

            if (row is not null)
                return row;

            await Task.Delay(500);
        }

        return null;
    }
}
