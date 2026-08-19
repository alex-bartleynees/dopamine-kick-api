using Entitlements.Domain;
using Entitlements.Domain.Entities;
using Entitlements.Infrastructure.DbContexts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace WebApi.IntegrationTests.Entitlements;

[Collection(IntegrationCollection.Name)]
public class SubscriptionAccessServiceTests(ApiTestFixture fixture)
{
    private const string ProductId = EntitlementProducts.DopamineKick;

    [Fact]
    public async Task Returns_true_when_entitlement_row_has_HasAccess_true()
    {
        var userId = Guid.NewGuid();
        await SeedEntitlementAsync(userId, hasAccess: true, status: "active");

        var result = await CheckAccessAsync(userId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_false_when_entitlement_row_has_HasAccess_false()
    {
        var userId = Guid.NewGuid();
        await SeedEntitlementAsync(userId, hasAccess: false, status: "canceled");

        var result = await CheckAccessAsync(userId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_false_when_no_entitlement_row_exists()
    {
        var userId = Guid.NewGuid(); // never seeded

        var result = await CheckAccessAsync(userId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_false_when_another_product_has_access()
    {
        var userId = Guid.NewGuid();
        await SeedEntitlementAsync(userId, hasAccess: true, status: "active", productId: "coffee_journal");

        var result = await CheckAccessAsync(userId);

        result.Should().BeFalse();
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task SeedEntitlementAsync(
        Guid userId,
        bool hasAccess,
        string status,
        string productId = ProductId)
    {
        await fixture.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<EntitlementsContext>();
            db.Entitlements.Add(new Entitlement
            {
                ProductId = productId,
                UserId = userId,
                HasAccess = hasAccess,
                Status = status,
            });
            await db.SaveChangesAsync();
        });
    }

    private async Task<bool> CheckAccessAsync(Guid userId)
    {
        var result = false;
        await fixture.WithScopeAsync(async sp =>
        {
            var svc = sp.GetRequiredService<ISubscriptionAccessService>();
            result = await svc.HasActiveAccessAsync(userId);
        });
        return result;
    }
}
