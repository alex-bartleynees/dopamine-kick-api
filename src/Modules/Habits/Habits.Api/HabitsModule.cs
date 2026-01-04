using Common.Abstractions;
using Common.Infrastructure.Interceptors;
using FluentValidation;
using Habits.Application.Abstractions;
using Habits.Application.Common.Models;
using Habits.Application.Common.Validators;
using Habits.Infrastructure.DbContexts;
using Habits.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Habits.Api;

public static class HabitsModule
{
    public static IServiceProvider MigrateHabitsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HabitsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddHabitsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("HabitsDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<HabitsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IHabitsRepository, HabitsRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HabitsContext>());

        services.AddScoped<IValidator<HabitForCreationDto>, HabitForCreationDtoValidator>();
        services.AddScoped<IValidator<BulkHabitsForCreationDto>, BulkHabitsForCreationDtoValidator>();
        services.AddScoped<HabitForCreationDtoValidator>();

        services.AddMediator(options =>
        {
            options.Namespace = "Habits.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        return services;
    }
}