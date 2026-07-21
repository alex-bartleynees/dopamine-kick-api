using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebApi.Telemetry;

public static class AppTelemetry
{
    public const string SourceName = "DopamineKick.API";

    public static class Tags
    {
        public const string Success = "success";
        public const string ErrorType = "error.type";
        public const string UserId = "user.id";
        public const string Module = "app.module";
    }

    public static readonly ActivitySource ActivitySource = new(SourceName);

    public static readonly Meter Meter = new(SourceName);
}
