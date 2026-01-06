using Habits.Application.Abstractions;
using Habits.Domain.Entities;
using Habits.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Habits.Infrastructure.DbContexts;

public class HabitsContext(DbContextOptions<HabitsContext> options) : DbContext(options), IHabitsUnitOfWork
{
    public DbSet<Habit> Habits { get; set; }

    public DbSet<HabitCompletion> HabitCompletions { get; set; }

    public DbSet<HabitReminder> HabitReminders { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HabitConfiguration).Assembly);
    }
}