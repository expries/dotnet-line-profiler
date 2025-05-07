using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SampleAspNetApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                if (request.Headers.ContainsKey("X-End-To-End-Test"))
                {
                    activity.AddTag("http.request.is_test", true);
                }
            };
        })
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    }));

var app = builder.Build();

app.MapGet("/", () =>
{
    var customerCount = CustomerService.GetNumberOfCustomers();
    
    switch (customerCount)
    {
        case < 20:
            Console.WriteLine("We found no customers");
            break;
        case < 40:
            Console.WriteLine("We found plenty customers");
            break;
        case < 60:
            Console.WriteLine("We found plenty customers");
            break;
        case < 80:
            Console.WriteLine("We found plenty customers");
            break;
        default:
            Console.WriteLine("We found plenty customers");
            break;
    }

    return "Customers: " + customerCount;
});

app.Run();