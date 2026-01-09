
using Common.IntegrationEvents.Habits;

namespace Notifications.Application.Abstractions;

public interface IJobScheduler
{
    public Task ScheduleHabitReminderAsync(HabitReminderCreated message);
}