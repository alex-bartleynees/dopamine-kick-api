using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Habits.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace WebApi.IntegrationTests.Habits;

[Collection(IntegrationCollection.Name)]
public class OutboxPublishingTests(ApiTestFixture fixture)
{
    private record CreateHabitRequest(string Name, string Emoji, string Target);

    private record HabitResponse(Guid Id);

    private record CreateReminderRequest(string NotificationTime, string TimeZone, string PreferredTime, bool IsEnabled);

    [Fact]
    public async Task Creating_an_enabled_reminder_publishes_the_outbox_message()
    {
        var userId = Guid.NewGuid();
        var client = fixture.CreateClientAs(userId);

        var createHabit = await client.PostAsJsonAsync(
            "/api/habits",
            new CreateHabitRequest("Hydrate", "💧", "8 glasses"));
        createHabit.StatusCode.Should().Be(HttpStatusCode.Created);
        var habit = (await createHabit.Content.ReadFromJsonAsync<HabitResponse>())!;

        var createReminder = await client.PostAsJsonAsync(
            $"/api/habits/{habit.Id}/reminders",
            new CreateReminderRequest("08:00:00", "UTC", "morning", IsEnabled: true));
        createReminder.StatusCode.Should().Be(HttpStatusCode.Created);

        // The OutboxPublisher polls every ~5s; wait for it to publish to the real RabbitMQ container.
        var published = await WaitForOutboxPublishedAsync(userId, TimeSpan.FromSeconds(20));

        published.Should().BeTrue("the OutboxPublisher should have published the HabitReminderCreated event to RabbitMQ");
    }

    private async Task<bool> WaitForOutboxPublishedAsync(Guid userId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var published = false;
            await fixture.WithScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<HabitsContext>();
                // The outbox payload embeds the user id, so match on it to isolate this test's message.
                published = await db.OutboxMessages
                    .AsNoTracking()
                    .AnyAsync(m => m.Published && m.Payload.Contains(userId.ToString()));
            });

            if (published)
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }
}
