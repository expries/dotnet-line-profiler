using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LineProfiler.Interception;

public static class Callbacks
{
    public static unsafe delegate* unmanaged<bool*, int, void> ProbePointer { get; } = &Probe;

    [UnmanagedCallersOnly]
    public static unsafe void Probe(bool* ptr, int length)
    {
        Console.WriteLine("Probe called!");
        Console.WriteLine("Length: " + length);

        Console.WriteLine("Profiler Trace id: " + Activity.Current?.Id);

        for (var i = 0; i < length; i++)
        {
            var value = ptr[i];
            Console.WriteLine($"Value at i={i}: {value}");
        }

        Console.WriteLine("");
    }
}