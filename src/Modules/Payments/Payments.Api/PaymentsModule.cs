using Payments.Domain.Billing;
using SharedKernel.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions;
using Payments.Infrastructure.BackgroundServices;
using Payments.Infrastructure.Configuration;
using Payments.Infrastructure.DbContexts;
using Payments.Infrastructure.Repositories;
using Payments.Infrastructure.Services;

namespace Payments.Api;

public static class PaymentsModule
{
    public static IServiceProvider MigratePaymentsDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsContext>();
        db.Database.Migrate();
        return services;
    }

    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("PaymentsDBConnectionString") ??
                 throw new ArgumentNullException(nameof(configuration), "No connection string provided");

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<PaymentsContext>((sp, options) =>
            options
                .UseNpgsql(cs, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        services.AddScoped<IPaymentsRepository, PaymentsRepository>();
        services.AddScoped<IPaymentsUnitOfWork>(sp => sp.GetRequiredService<PaymentsContext>());
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISubscriptionSyncService, SubscriptionSyncService>();

        // Shared cross-module entitlement read, consumed by the Host's RequireActiveSubscription filter.
        services.AddScoped<ISubscriptionAccessService, SubscriptionAccessService>();

        // Ack-fast webhook processing: the handler durably records events in the inbox; this poller
        // drains them in the background with SKIP LOCKED (safe across multiple instances).
        services.AddHostedService<InboxProcessor>();

        services.AddMediator(options =>
        {
            options.Namespace = "Payments.Api.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.GenerateTypesAsInternal = true;
        });

        return services;
    }
}
