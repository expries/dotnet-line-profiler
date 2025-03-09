using System.Runtime.InteropServices;

namespace Profiler.Core.Callbacks;

public class Callbacks
{
    [UnmanagedCallersOnly]
    public static void Probe(long functionId)
    {
        Console.WriteLine("Probe executed");
        Console.WriteLine("FunctionID: " +functionId );
    }
    
    public static void Probev2(long functionId)
    {
        Console.WriteLine("Probe executed");
        Console.WriteLine("FunctionID: " +functionId );
    }

    public unsafe delegate* unmanaged<long, void> ProbePointer = &Probe;
}