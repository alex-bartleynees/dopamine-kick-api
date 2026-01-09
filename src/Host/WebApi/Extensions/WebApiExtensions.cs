using Common.Abstractions;
using Common.Abstractions.Messaging;
using Common.Infrastructure.Messaging;
using Habits.Api;
using Microsoft.OpenApi;
using Notifications.Api;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using StackExchange.Redis;
using Users.Api;
using WebApi.Middleware;
using WebApi.ValidationErrors;

namespace WebApi.Extensions;

public static class WebApiExtensions
{
    public static void RegisterServices(this WebApplicationBuilder builder)
    {
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        // Add StackExchangeRedisCache service for Redis
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var redisConfig = ConfigurationOptions.Parse(
                builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379"
            );

            // Set password from configuration
            redisConfig.Password = builder.Configuration["Redis:Password"];

            // Optional: Configure TLS
            if (builder.Configuration.GetValue<bool>("Redis:UseTls"))
            {
                redisConfig.Ssl = true;
                redisConfig.SslHost = builder.Configuration["Redis:Host"];

                // Allow self-signed certificates (common in Kubernetes internal Redis)
                redisConfig.CertificateValidation += (sender, cert, chain, errors) => true;
            }

            // Retry configuration
            redisConfig.AbortOnConnectFail = false;
            redisConfig.ConnectRetry = 3;

            options.ConfigurationOptions = redisConfig;
            options.InstanceName = "DopamineKickApiCache:";
        });

        builder.Services.AddUsersModule(builder.Configuration);
        builder.Services.AddHabitsModule(builder.Configuration);
        builder.Services.AddNotificationsModule(builder.Configuration);

        // Register RabbitMQ Options and Services
        builder.Services.Configure<RabbitMqOptions>(
            builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();

        // Add HybridCache service
        builder.Services.AddHybridCache();

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
        builder.Services.AddProblemDetails();
        builder.Services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.OverrideDefaultResultFactoryWith<ValidationErrorFactory>();
        });

        builder.Services.AddAuthentication()
            .AddJwtBearer(options =>
                {
                    options.Authority = builder.Configuration["Jwt:Authority"]
                                        ?? throw new ArgumentNullException("Jwt:Authority",
                                            "JWT Authority must be configured");
                    options.Audience = builder.Configuration["Jwt:Audience"]
                                       ?? throw new ArgumentNullException("Jwt:Audience",
                                           "JWT Audience must be configured");
                }
            );

        builder.Services.AddAuthorizationBuilder();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "DopamineKick API",
                Description = "DopamineKick API documentation",
            });
        });
    }

    public static void RegisterAppConfig(this WebApplication app)
    {
        app.Services.MigrateUsersDatabase();
        app.Services.MigrateHabitsDatabase();
        app.Services.MigrateNotificationsDatabase();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        app.UseCors(MyAllowSpecificOrigins);

        app.RegisterEndpointDefinitions();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    private static void RegisterEndpointDefinitions(this WebApplication app)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.EndsWith(".Api") == true
                        || a.GetName().Name == "WebApi");

        var endpointDefinitions = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableTo(typeof(IEndpointDefinition)) && !t.IsAbstract && !t.IsInterface)
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            endpointDefinition.RegisterEndpoints(app);
        }
    }
}