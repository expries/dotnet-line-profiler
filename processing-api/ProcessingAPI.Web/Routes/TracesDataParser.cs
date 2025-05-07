using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using ProcessingAPI.Services.Models;
using Method = ProcessingAPI.Services.Models.Method;
using Models_Span = ProcessingAPI.Services.Models.Span;
using Span = OpenTelemetry.Proto.Trace.V1.Span;

namespace ProcessingAPI.Web.Routes;

public class TracesDataParser
{
    public static List<MethodCall> ParseMethodCalls(TracesData tracesData)
    {
        var methodCalls = new List<MethodCall>();
        
        foreach (var resourceSpan in tracesData.ResourceSpans)
        {
            foreach (var scopeScan in resourceSpan.ScopeSpans)
            {
                foreach (var span in scopeScan.Spans)
                {
                    var spanId = ByteStringToHexadecimalString(span.SpanId);
                    var traceId = ByteStringToHexadecimalString(span.TraceId);
                    var rootSpanId = ByteStringToHexadecimalString(span.ParentSpanId);
                    var spanStart = span.StartTimeUnixNano;
                    var spanEnd = span.EndTimeUnixNano;
                        
                    if (!TryParseSpanAttributes(span.Attributes, 
                            out var method, out var executedLines) || method is null)
                    {
                        continue;
                    }

                    var callSpan = new MethodCall
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Method = method,
                        Branches = ParseSpanEvents(span.Events),
                        ExecutedCodeLines = executedLines,
                        Span = new Models_Span
                        {
                            Id = spanId,
                            TraceId = traceId,
                            RootSpanId = rootSpanId,
                            StartTime = DateTimeExtensions.FromNanoseconds(spanStart),
                            EndTime = DateTimeExtensions.FromNanoseconds(spanEnd),
                        },
                    };
                    
                    methodCalls.Add(callSpan);
                }
            }
        }
        
        return methodCalls;
    }

    private static string ByteStringToHexadecimalString(ByteString byteString)
    {
        var byteArray = byteString.ToArray();
        var str = BitConverter.ToString(byteArray);
        return str.ToLowerInvariant();
    }
    
    private static bool TryParseSpanAttributes(
        RepeatedField<KeyValue> spanAttributes, out Method? method, out int[] executedLines)
    {
        method = null;
        executedLines = [];
        
        var newMethod = new Method
        {
            Id = Guid.NewGuid().ToString(),
            Revision = 1,
            Name = string.Empty,
            FullTypeName = string.Empty,
            SourceFileName = string.Empty,
            SourceCode = string.Empty,
            CodeStartLine = 0,
            CodeEndLine = 0,
        };

        foreach (var attribute in spanAttributes)
        {
            switch (attribute.Key)
            {
                case "source.text":
                    var sourceText = DecodeAsString(attribute.Value);
                    Console.WriteLine($"source.text:{sourceText}");
                    newMethod.SourceCode = sourceText;
                    break;
                case "source.name":
                    var sourceName = DecodeAsString(attribute.Value);
                    Console.WriteLine($"source.name:{sourceName}");
                    newMethod.SourceFileName = sourceName;
                    break;
                case "method.name":
                    var methodName = DecodeAsString(attribute.Value);
                    Console.WriteLine($"method.name:{methodName}");
                    newMethod.Name = methodName;
                    break;
                case "method.type_name":
                    var typeName = DecodeAsString(attribute.Value);
                    Console.WriteLine($"method.type_name:{typeName}");
                    newMethod.FullTypeName = typeName;
                    break;
                case "method.start_line":
                    Console.WriteLine($"method.start_line:{attribute.Value.IntValue}");
                    newMethod.CodeStartLine = attribute.Value.IntValue;
                    break;
                case "method.end_line":
                    Console.WriteLine($"method.end_line:{attribute.Value.IntValue}");
                    newMethod.CodeEndLine = attribute.Value.IntValue;
                    break;
                case "method.lines_executed":
                    var linesExecutedString = attribute.Value.StringValue;
                    executedLines = linesExecutedString.Split(",").Select(int.Parse).ToArray();
                    break;
            }
        }

        if (
            string.IsNullOrEmpty(newMethod.SourceFileName) ||
            string.IsNullOrEmpty(newMethod.SourceCode) ||
            string.IsNullOrEmpty(newMethod.Name) ||
            string.IsNullOrEmpty(newMethod.FullTypeName) ||
            newMethod is { CodeStartLine: 0, CodeEndLine: 0 })
        {
            return false;
        }
        
        method = newMethod;
        return true;
    }

    private static string DecodeAsString(AnyValue value)
    {
        return !value.HasStringValue ? string.Empty : value.StringValue.Replace("\0", "");
    }
    
    private static List<Branch> ParseSpanEvents(RepeatedField<Span.Types.Event> spanEvents)
    {
        var branches = new List<Branch>();
        
        foreach (var spanEvent in spanEvents)
        {
            if (TryParseSpanEventAttributes(spanEvent.Attributes, out var branch) && branch is not null)
            {
                branches.Add(branch);
            }
        }
        
        return branches;
    }
    
    private static bool TryParseSpanEventAttributes(
        RepeatedField<KeyValue> eventAttributes, 
        out Branch? branch)
    {
        var blockId = string.Empty;
        int[] branchLines = [];
        
        foreach (var attribute in eventAttributes)
        {
            switch (attribute.Key)
            {
                case "branch.id":
                    blockId = attribute.Value.StringValue;
                    break;
                case "branch.lines":
                    var branchLinesString = attribute.Value.StringValue;
                    branchLines = branchLinesString.Split(",").Select(int.Parse).ToArray();
                    break;
            }
        }

        if (string.IsNullOrEmpty(blockId) ||  branchLines.Length == 0)
        {
            branch = null;
            return false;
        }

        branch = new Branch
        {
            Id = Guid.NewGuid().ToString(),
            BlockId = blockId,
            Executions = 1,
            CodeLines = branchLines,
        };

        return true;
    }
}