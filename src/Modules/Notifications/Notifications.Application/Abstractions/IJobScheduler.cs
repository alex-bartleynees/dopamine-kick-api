
using Common.IntegrationEvents.Habits;
using Common.IntegrationEvents.Quests;

namespace Notifications.Application.Abstractions;

public interface IJobScheduler
{
    public Task ScheduleHabitReminderAsync(HabitReminderCreated message);

    public Task CancelHabitReminderAsync(Guid reminderId, Guid userId);

    public Task ScheduleQuestReminderAsync(QuestReminderCreated message);

    public Task CancelQuestReminderAsync(Guid reminderId, Guid userId);
}
