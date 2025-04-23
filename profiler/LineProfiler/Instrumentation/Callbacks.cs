using System.Runtime.InteropServices;
using IlRewriter.Models;
using IlRewriter.Models.Signatures;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public static class Callbacks
{
    public static unsafe delegate* unmanaged<bool*, int, nint, nint, IntPtr, void> ProbePointer { get; } = &Probe;
    
    public static readonly MethodSignature Signature = new (
        CorUnmanagedCallingConventionFlags.IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL,
        // Number of arguments
        CorElementTypeFlags.ELEMENT_TYPE_VOID, 5,
        // Bool array pointer
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN,
        // Bool array length
        CorElementTypeFlags.ELEMENT_TYPE_I,
        // Assembly id
        CorElementTypeFlags.ELEMENT_TYPE_I,
        // Function id
        CorElementTypeFlags.ELEMENT_TYPE_I, 
        // Trace id
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_CHAR);

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
        if (stringPointer == IntPtr.Zero) return null;
        var headerOffset = IntPtr.Size;
        var stringLength = Marshal.ReadInt32(stringPointer, headerOffset);
        if (stringLength < 0) return null;
        var offset = headerOffset + sizeof(int);
        return Marshal.PtrToStringAuto(stringPointer + offset, stringLength);
    }
}