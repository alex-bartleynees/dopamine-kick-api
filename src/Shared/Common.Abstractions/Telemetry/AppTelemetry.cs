using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Common.Abstractions.Telemetry;

/// <summary>
/// Central definitions for the application's custom telemetry. Automatic instrumentation
/// (ASP.NET Core, HttpClient, Npgsql, runtime) is wired up in the host; this type gives
/// application code a shared <see cref="ActivitySource"/> and <see cref="Meter"/> for
/// emitting custom spans and metrics that flow through the same OpenTelemetry pipeline.
/// </summary>
public static class AppTelemetry
{
    /// <summary>
    /// Name used for the service resource, the custom <see cref="ActivitySource"/> and the
    /// custom <see cref="Meter"/>. Registered as a trace source and meter in the host.
    /// </summary>
    public const string SourceName = "DopamineKick.API";

    /// <summary>Common tag names — prefer these over magic strings when tagging spans/metrics.</summary>
    public static class Tags
    {
        public const string Success = "success";
        public const string ErrorType = "error.type";
        public const string UserId = "user.id";
        public const string Module = "app.module";
    }

    /// <summary>Shared source for custom distributed-tracing spans.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>Shared meter for custom metrics.</summary>
    public static readonly Meter Meter = new(SourceName);
}
