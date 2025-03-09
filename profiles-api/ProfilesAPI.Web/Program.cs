using System.IO.Compression;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Profiles.V1Development;
using OpenTelemetry.Proto.Trace.V1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapPost("/api/v1/traces", async ctx =>
{
    Console.WriteLine("TRACE");
    var body = GetDecompressedRequestBody(ctx.Request);
    var tracesData = TracesData.Parser.ParseFrom(body);
    Console.WriteLine($"Received {tracesData.ResourceSpans.Count} traces");
});

app.MapPost("/api/v1/metrics", async ctx =>
{
    Console.WriteLine("METRICS");
    var body = GetDecompressedRequestBody(ctx.Request);
    var metricsData = MetricsData.Parser.ParseFrom(body);
    Console.WriteLine($"Received {metricsData.ResourceMetrics.Count} metrics");
});
    
app.MapPost("/api/v1development/profiles", async ctx =>
{
    Console.WriteLine("PROFILES");
    var body = GetDecompressedRequestBody(ctx.Request);
    var profilesData = ProfilesData.Parser.ParseFrom(body);
    Console.WriteLine($"Received {profilesData.ResourceProfiles} profiles");
});


app.Run();
return;

Stream GetDecompressedRequestBody(HttpRequest request)
{
    request.Headers.TryGetValue("Content-Encoding", out var encoding);

    if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
    {
        return new GZipStream(request.Body, CompressionMode.Decompress);
    }

    return request.Body;
}

