using System.IO.Compression;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Profiles.V1Development;
using OpenTelemetry.Proto.Trace.V1;
using ProcessingAPI.Services;

namespace ProcessingAPI.Web.Routes;

public static class OtelRoutes
{

    public static IEndpointRouteBuilder AddOtelRoutes(this IEndpointRouteBuilder app)
    {
        app .AddOtelTracesReceiver()
            .AddOtelMetricsReceiver()
            .AddOtelProfilesReceiver();
        return app;
    }

    public static IEndpointRouteBuilder AddOtelTracesReceiver(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/traces", async (HttpContext ctx, MethodCallService methodCallService) =>
        {
            Console.WriteLine("TRACE");
            var body = GetDecompressedRequestBody(ctx.Request);
            var tracesData = TracesData.Parser.ParseFrom(body);
            Console.WriteLine($"Received {tracesData.ResourceSpans.Count} traces");
            var methodCalls = TracesDataParser.ParseMethodCalls(tracesData);
            Console.WriteLine($"Ingested {methodCalls.Count} method calls");
            await methodCallService.ProcessMethodCallAsync(methodCalls);
        });
        return app;
    }
    
    public static IEndpointRouteBuilder AddOtelProfilesReceiver(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1development/profiles", (HttpContext ctx) =>
        {
            Console.WriteLine("PROFILES");
            var body = GetDecompressedRequestBody(ctx.Request);
            var profilesData = ProfilesData.Parser.ParseFrom(body);
            Console.WriteLine($"Received {profilesData.ResourceProfiles} profiles");
        });
        return app;
    }
    public static IEndpointRouteBuilder AddOtelMetricsReceiver(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/metrics", (HttpContext ctx) =>
        {
            Console.WriteLine("METRICS");
            var body = GetDecompressedRequestBody(ctx.Request);
            var metricsData = MetricsData.Parser.ParseFrom(body);
            Console.WriteLine($"Received {metricsData.ResourceMetrics.Count} metrics");
        });
        return app;
    }
    
    private static Stream GetDecompressedRequestBody(HttpRequest request)
    {
        request.Headers.TryGetValue("Content-Encoding", out var encoding);

        if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            return new GZipStream(request.Body, CompressionMode.Decompress);
        }

        return request.Body;
    }
}