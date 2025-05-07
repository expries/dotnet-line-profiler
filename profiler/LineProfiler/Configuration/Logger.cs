
namespace LineProfiler.Configuration;

public static class Logger
{
    public static void Info(string log)
    {
        LogInternal(log, "Info");
    }
    
    public static void Error(string log)
    {
        LogInternal(log, "Error");
    }
    
    private static void LogInternal(string line, string logLevel)
    {
        Console.WriteLine($"[Profiler][{logLevel}] {line}");
    }
}