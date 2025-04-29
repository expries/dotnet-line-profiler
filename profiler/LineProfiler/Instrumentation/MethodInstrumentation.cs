using IlRewriter.Models;
using IlRewriter.Models.Operands;
using IlRewriter.Models.Signatures;
using LineProfiler.Caches;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public static class MethodInstrumentation
{
    public static LocalVarSignature CreateLocalVarSignature(
        LocalVarSignature signature,
        MdTypeRef activityRef,
        MdTypeRef traceId)
    {
        // variable: 'bool*': which probes have been visited
        byte[] arrayPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        
        // variable 'Activity' (class-instance): current Activity
        var activityRid = Rid.FromTypeRef(activityRef.Value);
        var activitySignature = LocalVarSignature.CreateClassInstanceVariable(activityRid);
        
        // variable 'TraceId' (struct): current Trace ID
        var traceIdRid = Rid.FromTypeRef(traceId.Value);
        var traceIdSignature = LocalVarSignature.CreateValueTypeVariable(traceIdRid);

        // variable: 'char*': current trace id as string
        byte[] traceIdCPtrSignature =
        [
            CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_PTR,
            CorElementTypeFlags.ELEMENT_TYPE_CHAR
        ];
        
        signature.AddLocalVariables([arrayPtrSignature, activitySignature, traceIdSignature, traceIdCPtrSignature]);
        return signature;
    }

    public static Method InstrumentMethod(
        Method method,
        InstrumentationMetadata metadata)
    {
        var probes = GetAndCacheProbes(metadata.AssemblyId, metadata.FunctionId, method);
        var instrumentedMethod = AddInstrumentation(method, probes, metadata);
        return instrumentedMethod;
    }

    private static Dictionary<int, Probe> GetAndCacheProbes(
        AssemblyId assemblyId, FunctionId functionId, Method method)
    {
        var cfg = new ControlFlowGraph(method.Body.Instructions);
        var parser = new ControlFlowGraphParser(cfg);
        var probes = parser.LocateProbes();
        ProbeCache.AddMethodProbes(assemblyId, functionId, probes);
        return probes;
    }

    private static Method AddInstrumentation(Method method, Dictionary<int, Probe> probes, InstrumentationMetadata metadata)
    {
        var instructionOffset = 0;
        instructionOffset += AddInitialization(method, metadata.BoolType, probes.Count, metadata.LocalVarCount);

        var orderedProbes = probes
            .OrderBy(probe => probe.Key)
            .ToDictionary();

        var probeIndices = orderedProbes.Keys.ToList();

        for (var i = 0; i < orderedProbes.Count; i++)
        {
            var probeIndex = probeIndices[i];
            var probe = orderedProbes[probeIndex];

            switch (probe.Type)
            {
                case ProbeType.End:
                    instructionOffset += AddProbeForBranchEnd(
                        method, orderedProbes, i, probeIndex, metadata.LocalVarCount, instructionOffset);
                    break;
                case ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(method, orderedProbes, i, probeIndex,
                        instructionOffset, metadata);
                    break;
            }
        }

        const int instrumentationStackSize = 20;
        method.Header.MaxStack = (ushort)(method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint)metadata.SignatureTokens.LocalVars.Value;
        return method;
    }

    private static int AddInitialization(Method method, MdTypeRef boolType, int arrayLength, uint localVarCount)
    {
        var initArray = new InstructionBuilder()
            .CreateArray(boolType, arrayLength, localVarCount)
            .Build();

        method.Body.Instructions.InsertRange(0, initArray);
        return initArray.Count;
    }

    private static int AddProbeForBranchEnd(
        Method method,
        Dictionary<int, Probe> probes,
        int arrayIndex,
        int probeIndex,
        uint localVarCount,
        int instructionOffset)
    {
        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, localVarCount)
            .Build();

        var probe = probes[probeIndex];
        RewriteBranchesIfSet(probe, probeInstructions);

        var beforeBranchEndIndex = probeIndex + instructionOffset;
        method.Body.Instructions.InsertRange(beforeBranchEndIndex, probeInstructions);
        return probeInstructions.Count;
    }

    private static unsafe int AddProbeForReturnStatement(
        Method method,
        Dictionary<int, Probe> probes,
        int arrayIndex,
        int probeIndex,
        int instructionOffset,
        InstrumentationMetadata metadata)
    {
        var probePointer = new UIntPtr(Callbacks.ProbePointer);

        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, metadata.LocalVarCount)
            .AddMethodCall(probePointer, metadata, probes.Count)
            .Build();

        var probe = probes[probeIndex];
        RewriteBranchesIfSet(probe, probeInstructions);

        var beforeReturnIndex = probeIndex + instructionOffset;
        method.Body.Instructions.InsertRange(beforeReturnIndex, probeInstructions);
        return probeInstructions.Count;
    }

    private static void RewriteBranchesIfSet(Probe probe, List<Instruction> probeInstructions)
    {
        var startOfProbe = probeInstructions[0];

        foreach (var branch in probe.BranchesToRewrite)
        {
            RewriteTarget(branch, startOfProbe);
        }
    }

    private static void RewriteTarget(Probe.BranchToRewrite branch, Instruction target)
    {
        switch (branch.Instruction.Operand)
        {
            case InlineBrTargetOperand:
                branch.Instruction.Operand = new InlineBrTargetOperand { Instruction = target };
                return;
            case ShortInlineBrTargetOperand:
                branch.Instruction.Operand = new ShortInlineBrTargetOperand { Instruction = target };
                return;
            case InlineSwitchOperand switchOperand:
                var switchIndex = branch.SwitchIndex;
                if (!switchIndex.HasValue) return;
                switchOperand.Branches[switchIndex.Value] = target;
                return;
        }
    }
}