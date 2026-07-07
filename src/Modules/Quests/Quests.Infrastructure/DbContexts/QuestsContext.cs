using Microsoft.EntityFrameworkCore;
using Quests.Application.Abstractions;
using Quests.Domain.Entities;
using Quests.Infrastructure.Configuration;

namespace Quests.Infrastructure.DbContexts;

public class QuestsContext(DbContextOptions<QuestsContext> options) : DbContext(options), IQuestsUnitOfWork
{
    public DbSet<Quest> Quests { get; set; }

    public DbSet<QuestReminder> QuestReminders { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuestConfiguration).Assembly);
    }
}
