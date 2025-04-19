using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LineProfiler.Interception;

public static class Callbacks
{
    public static unsafe delegate* unmanaged<bool*, int, int, int, char*, void> ProbePointer { get; } = &Probe;

    [UnmanagedCallersOnly]
    public static unsafe void Probe(
        bool* pArray, 
        int length, 
        int assemblyId, 
        int functionId, 
        char* ptraceId)
    {
        Console.WriteLine("Probe called!");
        Console.WriteLine("Length: " + length);
        Console.WriteLine("AssemblyId: " + assemblyId);
        Console.WriteLine("FunctionId: " + functionId);
        
        var traceId = ptraceId is null ? null : new string(ptraceId);

        Console.WriteLine("Profiler Trace id: " + traceId);

        for (var i = 0; i < length; i++)
        {
            var value = pArray[i];
            Console.WriteLine($"Value at i={i}: {value}");
        }

        Console.WriteLine("");
    }
}