using System.Runtime.InteropServices;
using IlRewriter.Models;
using IlRewriter.Models.Signatures;
using LineProfiler.Caches;
using LineProfiler.DebugInfo;
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
        nint nAssemblyId, 
        nint nFunctionId, 
        IntPtr ptraceId)
    {
        var traceId = RawStringPointerToString(ptraceId);
        Console.WriteLine();

        if (string.IsNullOrEmpty(traceId))
        {
            Console.WriteLine("Empty or invalid trace id ...");
            return;
        }
        
        Console.WriteLine($"AssemblyId: {nAssemblyId}");
        Console.WriteLine($"FunctionId: {nFunctionId}");
        Console.WriteLine($"Trace id: {traceId}");

        var assemblyId = new AssemblyId(nAssemblyId);
        var functionId = new FunctionId(nFunctionId);
        
        var pdbExists = PdbCache.TryGetModule(assemblyId, out var pdbModule);

        if (!pdbExists || pdbModule is null)
        {
            Console.WriteLine($"No pdb module found for assembly id '{assemblyId.Value}'");
            return;
        }
        
        var functionExists = pdbModule.TryGetFunctionById(functionId.Value, out var pdbFunction);

        if (!functionExists || pdbFunction is null)
        {
            Console.WriteLine($"No pdb function found for function id '{functionId.Value}' in assembly id '{assemblyId.Value}'");
            return;
        }
        
        var probesExist = ProbeCache.TryGetProbes(assemblyId, functionId, out var probes);

        if (!probesExist || probes is null || probes.Count == 0)
        {
            Console.WriteLine($"No probes found for method assembly id '{assemblyId.Value}' and function id '{functionId.Value}'");
            return;
        }
        
        var linesByOffset = new Dictionary<int, List<SourceLine>>();


        for (var i = 0; i < length; i++)
        {
            var probeWasVisited = pArray[i];
            Console.WriteLine($"[{i}]: {probeWasVisited}");

            if (!probeWasVisited)
            {
                continue;
            }
            
            var probe = probes.Values.ToArray()[i];
            var coveredOffsets = probe.CoveredInstructionOffsets;

            foreach (var offset in coveredOffsets)
            {
                /*
                Console.WriteLine("Offset: " + offset);
                foreach (var (sequenceOffset, sequenceIterator) in pdbFunction.SequencesByOffset)
                {
                    var lineNumbersConcatenated = string.Join(',', sequenceIterator.Lines.Select(l => l.LineNumber));
                    Console.WriteLine($"{sequenceOffset}: {lineNumbersConcatenated}");
                }
                */
                
                var sequenceFound = pdbFunction.TryGetSequenceByOffset(offset, out var sequence);

                if (!sequenceFound || sequence is null)
                {
                    //Console.WriteLine($"No sequence found for sequence offset {offset}");
                    continue;
                }
                
                linesByOffset[offset] = sequence.Lines;
            }
        }

        var x = linesByOffset.OrderBy(kv => kv.Key).DistinctBy(x => x.Key).ToDictionary();

        foreach (var line in x.Values.SelectMany(v => v))
        {
            Console.WriteLine($"[{line.LineNumber}]: {string.Join(", ", line.Text)}");
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