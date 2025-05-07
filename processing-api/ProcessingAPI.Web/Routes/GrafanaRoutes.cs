using System.Net;
using System.Text.Json;
using ProcessingAPI.Services;
using ProcessingAPI.Services.Models;
using ProcessingAPI.Web.Dtos;

namespace ProcessingAPI.Web.Routes;

public static class GrafanaRoutes
{
    public static IEndpointRouteBuilder AddGrafanaRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/grafana/methods", async (HttpContext ctx, MethodService methodService) =>
        {
            var methods = await methodService.GetAllMethodsAsync();
            var response = methods
                .Select(method => new MethodDto
                {
                    Id = method.Id,
                    FullTypeName = method.FullTypeName,
                    Name = method.Name,
                }).ToArray();

            SetJsonResponse(ctx, response);
        });

        app.MapGet("/api/grafana/methods/calls", async (HttpContext ctx, MethodCallService callService) =>
        {
            var methodCalls = await callService.GetMethodCallsAsync();
            var result = methodCalls
                .Select(call => new MethodCallDto
                {
                    Method = new MethodDto
                    {
                        Id = call.Method.Id,
                        FullTypeName = call.Method.FullTypeName,
                        Name = call.Method.Name,
                    },
                    CallId = call.Id,
                    SpanId = call.Span.Id,
                    TraceId = call.Span.TraceId,
                    SpanStart = call.Span.StartTime,
                    SpanEnd = call.Span.EndTime,
                    Timestamp = call.Span.StartTime,
                }).ToArray();

            SetJsonResponse(ctx, result);
        });

        app.MapGet("/api/grafana/methods/statistics",
            async (HttpContext ctx, MethodService methodService, MethodCallService callService) =>
            {
                var methods = await methodService.GetAllMethodsAsync();
                var totalTraces = await callService.CountTotalTracesAsync();
                var methodStatistics = new List<MethodStatisticsDto>();

                foreach (var method in methods)
                {
                    var callsByTrace = await callService.CountMethodCallsDistinctByTraceIdByMethodIdAsync(method.Id);
                    var hotness = GetFractionInPercentage(callsByTrace, totalTraces);
                    methodStatistics.Add(new MethodStatisticsDto
                    {
                        Method = new MethodDto
                        {
                            Id = method.Id,
                            FullTypeName = method.FullTypeName,
                            Name = method.Name,
                        },
                        Hotness = hotness,
                        TotalExecutions = method.Calls.Count
                    });
                }

                SetJsonResponse(ctx, methodStatistics);
            });

        app.MapGet("/api/grafana/methods/calls/{callId}/coverage",
            async (string callId, HttpContext ctx, MethodCallService callService) =>
            {
                var call = await callService.GetMethodCallByIdAsync(callId);

                if (call is null)
                {
                    throw new ArgumentException("Method call found", nameof(callId));
                }

                var code = call.Method.SourceCode;
                var codeLines = code.Split(Environment.NewLine);

                var result = new MethodCallCoverageDto
                {
                    Id = callId,
                    Method = new MethodDto
                    {
                        Id = call.Method.Id,
                        FullTypeName = call.Method.FullTypeName,
                        Name = call.Method.Name,
                    },
                    Lines = GetLineNumbers(call.Method).Select(lineNumber => new MethodCallCoverageDto.LineCoverage
                    {
                        Executed = call.ExecutedCodeLines.Any(number => number == lineNumber),
                        LineText = codeLines[lineNumber - 1],
                        LineNumber = lineNumber,
                    }).ToList()
                };

                SetJsonResponse(ctx, result);
            });

        app.MapGet("/api/grafana/methods/{methodId}/coverage",
            async (string methodId, HttpContext ctx, MethodService methodService) =>
            {
                var method = await methodService.GetMethodByIdAsync(methodId);

                if (method is null)
                {
                    throw new ArgumentException("Method not found", nameof(methodId));
                }

                var code = method.SourceCode;
                var codeLines = code.Split(Environment.NewLine);

                var lineCoverage = new List<MethodOverallCoverageDto.LineCoverage>();
                var methodExecutions = method.Calls.Count;
                var executedLines = method.Calls.First().ExecutedCodeLines;

                foreach (var branch in method.Branches)
                {
                    var executedRelative = GetFractionInPercentage(branch.Executions, methodExecutions);
                    var branchLineCoverage = branch.CodeLines.Select(lineNumber =>
                        new MethodOverallCoverageDto.LineCoverage
                        {
                            LineNumber = lineNumber,
                            LineText = codeLines[lineNumber - 1],
                            ExecutedRelative = executedRelative,
                            Tested = branch.LastTested is not null
                        }).ToArray();
                    lineCoverage.AddRange(branchLineCoverage);
                }

                GetLineNumbers(method).ForEach(lineNumber =>
                {
                    var coveredByBranch = lineCoverage.Any(l => l.LineNumber == lineNumber);
                    if (coveredByBranch) return;
                    lineCoverage.AddRange(new MethodOverallCoverageDto.LineCoverage
                    {
                        ExecutedRelative = executedLines.Contains(lineNumber) ? 100 : 0,
                        LineNumber = lineNumber,
                        LineText = codeLines[lineNumber - 1],
                    });
                });

                lineCoverage.Sort((a, b) => a.LineNumber - b.LineNumber);

                var coverage = new MethodOverallCoverageDto
                {
                    Method = new MethodDto
                    {
                        Id = method.Id,
                        FullTypeName = method.FullTypeName,
                        Name = method.Name,
                    },
                    Lines = lineCoverage
                };

                SetJsonResponse(ctx, coverage);
            });

        return app;
    }

    private static List<int> GetLineNumbers(Method method)
    {
        var start = (int)method.CodeStartLine - 1;
        var end = (int)method.CodeEndLine;
        var count = end - start + 1;
        return Enumerable
            .Range(start, count)
            .ToList();
    }

    private static double GetFractionInPercentage(int fraction, int total)
    {
        var result = Math.Round((double)fraction / total, 4);
        return result * 100;
    }

    private static void SetJsonResponse(HttpContext ctx, object response)
    {
        var json = JsonSerializer.Serialize(response,
            options: new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentType = "application/json";
        ctx.Response.Body.Write(bytes, 0, bytes.Length);
    }
}