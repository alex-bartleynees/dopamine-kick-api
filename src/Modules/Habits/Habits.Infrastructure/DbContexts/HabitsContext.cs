using Habits.Domain.Entities;
using Habits.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Habits.Infrastructure.DbContexts;

public class HabitsContext(IConfiguration configuration) : DbContext
{
    public DbSet<Habit> Habits { get; set; }

    public DbSet<HabitCompletion> HabitCompletions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(configuration.GetConnectionString("HabitsDBConnectionString") ??
                          throw new ArgumentNullException(nameof(options), "No connection string provided"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HabitConfiguration).Assembly);
    }
}