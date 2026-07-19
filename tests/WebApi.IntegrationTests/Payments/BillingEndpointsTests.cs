using System.Net;
using System.Net.Http.Json;
using Common.Abstractions.Billing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Payments.Domain.Billing;
using Payments.Domain.Entities;
using Payments.Infrastructure.DbContexts;
using WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace WebApi.IntegrationTests.Payments;

[Collection(IntegrationCollection.Name)]
public class BillingEndpointsTests(ApiTestFixture fixture)
{
    private record SubscriptionStateResponse(
        string Status,
        string? PriceId,
        DateTimeOffset? CurrentPeriodEnd,
        bool CancelAtPeriodEnd,
        string? PaymentMethodBrand,
        string? PaymentMethodLast4);

    [Fact]
    public async Task Subscription_state_requires_authentication()
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.GetAsync("/api/billing/subscription");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Portal_requires_authentication()
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.PostAsync("/api/billing/portal", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Subscription_state_for_new_user_is_none()
    {
        var client = fixture.CreateClientAs(Guid.NewGuid());

        var response = await client.GetAsync("/api/billing/subscription");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await response.Content.ReadFromJsonAsync<SubscriptionStateResponse>();
        state!.Status.Should().Be("none");
        state.CancelAtPeriodEnd.Should().BeFalse();
        state.PriceId.Should().BeNull();
    }

    [Fact]
    public async Task Subscription_state_serializes_status_as_the_contract_token()
    {
        // The domain models status as the SubscriptionStatus enum, but the frontend contract requires
        // the raw lower-case token. Seed a PastDue state and prove it round-trips through the EF value
        // converter (column → enum) and JSON (enum → token) as "past_due", and that it grants access.
        var userId = Guid.NewGuid();
        var customerReference = $"cus_test_{Guid.NewGuid():N}";

        await fixture.WithScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<PaymentsContext>();
            context.CustomerMappings.Add(new CustomerMapping(userId, customerReference));
            context.SubscriptionStates.Add(new SubscriptionState
            {
                CustomerReference = customerReference,
                Status = SubscriptionStatus.PastDue
            });
            await context.SaveChangesAsync();
        });

        var client = fixture.CreateClientAs(userId);
        var response = await client.GetAsync("/api/billing/subscription");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await response.Content.ReadFromJsonAsync<SubscriptionStateResponse>();
        state!.Status.Should().Be("past_due", "the wire contract uses Stripe's lower-case status token");

        await fixture.WithScopeAsync(async sp =>
        {
            var access = sp.GetRequiredService<ISubscriptionAccessService>();
            (await access.HasActiveAccessAsync(userId)).Should().BeTrue("past_due is a soft-grace access state");
        });
    }

    [Fact]
    public async Task Webhook_is_anonymous_and_rejects_an_invalid_signature()
    {
        // No auth header at all — the endpoint must be reachable (signature is the auth), and an
        // unverifiable payload must be rejected with 400, never 401.
        var client = fixture.Factory.CreateClient();

        var content = new StringContent(
            "{\"id\":\"evt_test\",\"type\":\"customer.subscription.updated\"}");
        content.Headers.Add("Stripe-Signature", "t=1,v1=not_a_valid_signature");

        var response = await client.PostAsync("/api/billing/webhook", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
