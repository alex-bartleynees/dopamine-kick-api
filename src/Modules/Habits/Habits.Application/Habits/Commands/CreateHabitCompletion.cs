using Common.Abstractions.Results;
using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Mediator;

namespace Habits.Application.Habits.Commands;

public record CreateHabitCompletion(Guid UserId, Guid HabitId, string Timezone) : IRequest<Result<HabitCompletion>>;

public class CreateHabitCompletionHandler(IHabitsRepository habitsRepository, IHabitsUnitOfWork unitOfWork)
    : IRequestHandler<CreateHabitCompletion, Result<HabitCompletion>>
{
    public async ValueTask<Result<HabitCompletion>> Handle(CreateHabitCompletion request, CancellationToken cancellationToken)
    {
        var habitEntity = await habitsRepository.GetHabitByIdAsync(request.UserId, request.HabitId, cancellationToken);
        if (habitEntity == null)
        {
            return Result<HabitCompletion>.Failure(new Error(404, "Not Found", $"Habit with id: {request.HabitId} was not found"));
        }

        var now = DateTimeOffset.UtcNow;
        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(request.Timezone);
        var userLocalTime = TimeZoneInfo.ConvertTime(now, userTimeZone);
        var today = DateOnly.FromDateTime(userLocalTime.DateTime);
        var habitForCompletion = new HabitCompletion
        {
            HabitId = request.HabitId,
            CompletedDate = today,
            CompletedAt = now
        };

        await habitsRepository.CreateHabitCompletionAsync(habitForCompletion, cancellationToken);
        RecordCompletion(habitEntity, today);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HabitCompletion>.Success(habitForCompletion);
    }

    private static void RecordCompletion(Habit habit, DateOnly completedDate)
    {
        var isConsecutive = habit.LastCompletedDate == completedDate.AddDays(-1);

        habit.CurrentStreak = isConsecutive ? habit.CurrentStreak + 1 : 1;
        habit.LongestStreak = Math.Max(habit.LongestStreak, habit.CurrentStreak);
        habit.LastCompletedDate = completedDate;
    }
}