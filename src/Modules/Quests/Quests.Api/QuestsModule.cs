using SharedKernel.EntityFrameworkCore;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quests.Application.Abstractions;
using Quests.Application.Common.Models;
using Quests.Application.Common.Validators;
using Quests.Infrastructure.BackgroundServices;
using Quests.Infrastructure.DbContexts;
using Quests.Infrastructure.Repositories;

namespace Quests.Api;

public static class QuestsModule
{
    public static IServiceProvider MigrateQuestsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuestsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddQuestsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("QuestsDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<QuestsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IQuestsRepository, QuestsRepository>();
        services.AddScoped<IQuestsUnitOfWork>(sp => sp.GetRequiredService<QuestsContext>());

        services.AddScoped<IValidator<QuestForCreationDto>, QuestForCreationDtoValidator>();
        services.AddScoped<IValidator<QuestForUpdateDto>, QuestForUpdateDtoValidator>();
        services.AddScoped<IValidator<QuestReminderForCreationDto>, QuestReminderForCreationDtoValidator>();

        services.AddMediator(options =>
        {
            options.Namespace = "Quests.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.GenerateTypesAsInternal = true;
        });

        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
