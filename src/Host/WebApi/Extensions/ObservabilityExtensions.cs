using WebApi.Telemetry;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebApi.Extensions;

/// <summary>
/// Configures OpenTelemetry logs, metrics and traces and exports them over OTLP to the
/// collector configured via the <c>OTLP_Endpoint</c> connection string
/// (defaults to the standard <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable when unset).
/// </summary>
public static class ObservabilityExtensions
{
    public static void AddObservability(this WebApplicationBuilder builder)
    {
        // Propagate W3C trace context + baggage across process boundaries, including through
        // RabbitMQ message headers so publish/consume spans link into one distributed trace.
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
        [
            new TraceContextPropagator(),
            new BaggagePropagator(),
        ]));

        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: AppTelemetry.SourceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName),
            });

        // Route ILogger output through OpenTelemetry so logs are correlated with traces.
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: AppTelemetry.SourceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", builder.Environment.EnvironmentName),
                }))
            .WithTracing(tracing => tracing
                .AddSource(AppTelemetry.SourceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Don't create spans for health-check polling noise.
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                // Npgsql (used by all EF Core module contexts) — traces DB commands.
                // Resolves to Npgsql.TracerProviderBuilderExtensions.AddNpgsql (via using Npgsql),
                // not EF Core's AddNpgsql<TContext> which is on IServiceCollection.
                .AddNpgsql()
                // RabbitMQ.Client 7 built-in publish/consume spans + header context propagation.
                .AddRabbitMQInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(AppTelemetry.SourceName)
                .AddNpgsqlInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        // Export everything (logs, metrics, traces) over OTLP. If the OTLP_Endpoint
        // connection string is set, use it (gRPC on :4317); otherwise fall back to the
        // standard OTEL_EXPORTER_OTLP_* environment variables.
        var otlpEndpoint = builder.Configuration.GetConnectionString("OTLP_Endpoint");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otlpEndpoint));
        }
        else
        {
            otel.UseOtlpExporter();
        }
    }
}
