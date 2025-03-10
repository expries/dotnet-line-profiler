using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OtelLineProfiler.Interception;

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
        
        
        /*
        Console.WriteLine($"123: {(int) data}");
        var memoryStream = TypeHelper.GetStreamFromBytePointer(data, length);
        memoryStream.ToArray().ToList().ForEach(b => Console.WriteLine(b));
        
        //Console.WriteLine($"Entering instruction {instructionOffset}");
        var pdb = new Pdb(@"C:\Coding\CodeLevelTracing\SampleConsoleApp\obj\Debug\net8.0\SampleConsoleApp.pdb");
        var x = pdb.GetLineInfoByAssemblyAndInstructionOffset();
        */
    }
}