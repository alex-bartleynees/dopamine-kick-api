using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Configuration;

namespace Notifications.Infrastructure.DbContexts;

public class NotificationsContext(DbContextOptions<NotificationsContext> options) : DbContext(options), INotificationsUnitOfWork
{
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
    public DbSet<WebPushSubscription> WebPushSubscriptions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessedMessageConfiguration).Assembly);
        
        // Adds Quartz.NET PostgreSQL schema to EntityFrameworkCore
        modelBuilder.AddQuartz(options => options.UsePostgreSql());

    } 
}