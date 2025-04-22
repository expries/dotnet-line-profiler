using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LineProfiler.Interception;

public static class Callbacks
{
    public static unsafe delegate* unmanaged<bool*, int, nint, nint, IntPtr, void> ProbePointer { get; } = &Probe;

    [UnmanagedCallersOnly]
    public static unsafe void Probe(
        bool* pArray, 
        int length, 
        nint assemblyId, 
        nint functionId, 
        IntPtr ptraceId)
    {
        var traceId = RawStringPointerToString(ptraceId);
        Console.WriteLine();

        if (string.IsNullOrEmpty(traceId))
        {
            Console.WriteLine("Empty or invalid trace id ...");
            return;
        }

        Console.WriteLine($"AssemblyId: {assemblyId}");
        Console.WriteLine($"FunctionId: {functionId}");
        Console.WriteLine($"Trace id: {traceId}");

        for (var i = 0; i < length; i++)
        {
            var value = pArray[i];
            Console.WriteLine($"[{i}]: {value}");
        }

        Console.WriteLine();
    }

    private static string? RawStringPointerToString(IntPtr stringPointer)
    {
        var headerOffset = IntPtr.Size;
        var length = Marshal.ReadInt32(stringPointer, headerOffset);
        var offset = headerOffset + sizeof(int);
        return Marshal.PtrToStringAuto(stringPointer + offset, length);
    }
}