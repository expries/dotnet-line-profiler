using IlRewriter;
using IlRewriter.Models;
using IlRewriter.Models.Operands;
using IlRewriter.Models.Signatures;
using LineProfiler.Caches;
using LineProfiler.Configuration;
using LineProfiler.DebugInfo;
using LineProfiler.Instrumentation.Metadata;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public static class MethodInstrumentation
{
    public static LocalVarSignature CreateLocalVarSignature(LocalVarSignature signature)
    {
        // variable: 'bool*': visited probes
        byte[] arrayPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        signature.AddLocalVariables([arrayPtrSignature]);
        return signature;
    }

    public static Method InstrumentMethod(
        Method method,
        PdbFunction symbols,
        InstrumentationMetadata metadata)
    {
        var probes = GetAndCacheProbes(metadata.FunctionToken, method, symbols);
        var instrumentedMethod = AddInstrumentation(method, probes, metadata);
        return instrumentedMethod;
    }

    private static Dictionary<int, Probe> GetAndCacheProbes(
        MdToken methodToken, Method method, PdbFunction symbols)
    {
        var cfg = new ControlFlowGraph(method.Body.Instructions);
        var parser = new ControlFlowGraphParser(cfg);
        var probes = parser.LocateProbes();
        var cachedProbes = new Dictionary<int, ProbeCacheEntry>();

        foreach (var (i, probe) in probes)
        {
            var coveredOffsets = probe.CoveredInstructionOffsets.ToArray();
            var blockOffsets = probe.Block.InstructionOffsets.ToArray();

            var (coveredLines, _) = symbols.GetLinesByInstructionOffsets(coveredOffsets);
            var (blockLines, _) = symbols.GetLinesByInstructionOffsets(blockOffsets);
            
            cachedProbes[i] = new ProbeCacheEntry(
                probe.Block.Id, 
                coveredLines.ToArray(),
                blockLines.ToArray());
        }
        
        CallbackCache.Instance.Probes[methodToken.Value] = cachedProbes;
        return probes;
    }

    private static Method AddInstrumentation(Method method, Dictionary<int, Probe> probes,
        InstrumentationMetadata metadata)
    {
        var instructionOffset = 0;
        instructionOffset += AddInitialization(method, metadata.BoolType, probes.Count, metadata.LocalVarCount);

        var probeIndices = probes.Keys.ToList();

        for (var i = 0; i < probes.Count; i++)
        {
            var probeIndex = probeIndices[i];
            var probe = probes[probeIndex];

            switch (probe.Type)
            {
                case ProbeType.End:
                    instructionOffset += AddProbeForBranchEnd(
                        method, probes, i, probeIndex, metadata.LocalVarCount, instructionOffset);
                    break;
                case ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(
                        method, probes, i, probeIndex, instructionOffset, metadata);
                    break;
            }
        }

        const int instrumentationStackSize = 20;
        method.Header.MaxStack = (ushort)(method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint)metadata.LocalVarSignature.Value;
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

    private static int AddProbeForReturnStatement(
        Method method,
        Dictionary<int, Probe> probes,
        int arrayIndex,
        int probeIndex,
        int instructionOffset,
        InstrumentationMetadata metadata)
    {
        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, metadata.LocalVarCount)
            .AddMethodCall(metadata, probes.Count)
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