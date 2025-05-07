using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace LineProfiler.Benchmarks;

[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
[Config(typeof(ConfigWithProfilerAttached))]
public class MethodBenchmarks
{
    private static Activity? MyActivity = Activity.Current;
    private static string? TraceId = MyActivity?.TraceId.ToHexString();
    
    private class ConfigWithProfilerAttached : ManualConfig
    {
        private const string PROFILER_DLL_PATH = 
            @"C:\Coding\otel-line-profiler\profiler\Release\win-x64\LineProfiler.dll";

        private const string PROFILER_PDB_DIRECTORY =
            @"C:\Coding\otel-line-profiler\profiler\LineProfiler.Benchmarks\bin\Release\net9.0";
            
        private static readonly EnvironmentVariable ProfilerEnabled = 
            new("CORECLR_ENABLE_PROFILING", "1");
        
        private static readonly EnvironmentVariable ProfilerClsId = 
            new("CORECLR_PROFILER", "{36032161-FFC0-4B61-B559-F6C5D41BAE5A}");
        
        private static readonly EnvironmentVariable ProfilerPath = 
            new("CORECLR_PROFILER_PATH", PROFILER_DLL_PATH);
        
        private static readonly EnvironmentVariable ProfilerPath64 = 
            new("CORECLR_PROFILER_PATH_64", PROFILER_DLL_PATH);
        
        private static readonly EnvironmentVariable ProfilerPdbDirectory = 
            new("PROFILER_PDB_DIRECTORY", PROFILER_PDB_DIRECTORY);

        public ConfigWithProfilerAttached()
        {
            AddJob(
                Job.Default
                    .WithEnvironmentVariables(
                        ProfilerEnabled,
                        ProfilerClsId, 
                        ProfilerPath, 
                        ProfilerPath64, 
                        ProfilerPdbDirectory)
                    .WithId("With profiler"));
            
            AddJob(
                Job.Default
                    .WithId("Without profiler"));
        }
    }
    
    [IterationSetup]
    public void SetupActivity()
    {
        var activity = new Activity("BenchmarkRun");
        activity.Start();
    }
    
    [IterationCleanup]
    public void StopActivity()
    {
        Activity.Current?.Stop();
    }
    
    [Benchmark]
    public void Method()
    {
        var value = Random.Shared.Next(0, 1000);

        if (value % 2 == 0)
        {
            Console.WriteLine("even: " + bool.TrueString);
            return;
        }

        Console.WriteLine("wow");
    }
}