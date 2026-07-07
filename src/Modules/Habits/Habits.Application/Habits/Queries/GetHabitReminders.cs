using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Queries;

public record GetHabitReminders(Guid UserId, Guid HabitId) : IRequest<Result<List<HabitReminder>>>;

public class GetHabitRemindersHandler(IHabitsRepository habitsRepository)
    : IRequestHandler<GetHabitReminders, Result<List<HabitReminder>>>
{
    public async ValueTask<Result<List<HabitReminder>>> Handle(GetHabitReminders request, CancellationToken cancellationToken)
    {
        var reminders = await habitsRepository.GetRemindersByHabitAsync(request.UserId, request.HabitId, cancellationToken);
        return Result<List<HabitReminder>>.Success(reminders);
    }
}
