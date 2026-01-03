using Common.Infrastructure.Interceptors;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Abstractions;
using Users.Application.Common.Models;
using Users.Application.Common.Validators;
using Users.Infrastructure.Configuration;
using Users.Infrastructure.DbContexts;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.Services;

namespace Users.Api;

public static class UsersModule
{
    public static IServiceProvider MigrateUsersDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var cs = configuration.GetConnectionString("UsersDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<UsersContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        // Keycloak HTTP Client
        services.Configure<KeycloakSettings>(configuration.GetSection("Keycloak"));
        services.AddHttpClient<IKeycloakService, KeycloakService>("KeycloakClient", client =>
        {
            var keycloakBaseUrl = configuration["Keycloak:BaseUrl"]
                                  ?? throw new ArgumentNullException(nameof(services),
                                      "Keycloak BaseUrl must be configured");
            client.BaseAddress = new Uri(keycloakBaseUrl);
        });

        services.AddScoped<IUsersRepository, UsersRepository>();

        services.AddScoped<IValidator<UserForCreationDto>, UserForCreationDtoValidator>();

        services.AddMediator(options =>
        {
            options.Namespace = "Users.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
     
        return services;
    }
}