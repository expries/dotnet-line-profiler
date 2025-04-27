using IlRewriter.Models;
using IlRewriter.Models.Operands;
using IlRewriter.Models.Signatures;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Instrumentation;



public static class Instrumentation
{
    public static LocalVarSignature CreateLocalVarSignature(
        LocalVarSignature signature, 
        MdTypeRef activityRef, 
        MdTypeRef traceId)
    {
        // TODO: modify signature in one iteration
        byte[] arrayPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        signature.AddLocalVariable(arrayPtrSignature);
        
        var activityRid = Rid.FromTypeRef(activityRef.Value);
        signature.AddInstanceVariable(activityRid, CorElementTypeFlags.ELEMENT_TYPE_CLASS);

        var traceIdRid = Rid.FromTypeRef(traceId.Value);
        signature.AddInstanceVariable(traceIdRid, CorElementTypeFlags.ELEMENT_TYPE_VALUETYPE);

        byte[] traceIdCPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_CHAR];
        signature.AddLocalVariable(traceIdCPtrSignature);
        return signature;
    }
    
    public static Method InstrumentMethod(
        Method method, 
        Dictionary<int, Probe> probes,
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
                        method, probes,i,  probeIndex, metadata.LocalVarCount, instructionOffset);
                    break;
                case ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(
                        method, probes, i, probeIndex, metadata.LocalVarCount, instructionOffset, metadata);
                    break;
            }
        }

        const int instrumentationStackSize = 6;
        method.Header.MaxStack = (ushort) (method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint) metadata.LocalVarSignature.Value;
        
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
        uint localVarCount, 
        int instructionOffset,
        InstrumentationMetadata metadata)
    {
        var probePointer = new UIntPtr(Callbacks.ProbePointer);
            
        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, localVarCount)
            .AddMethodCall(
                probePointer, 
                metadata,
                localVarCount, 
                probes.Count)
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

        foreach (var instruction in probe.BranchesToRewrite)
        {
            ChangeTarget(instruction, startOfProbe);
        }   
    }
    
    private static void ChangeTarget(Instruction from, Instruction to)
    {
        switch (from.Operand)
        {
            case InlineBrTargetOperand:
                from.Operand = new InlineBrTargetOperand { Instruction = to };
                return;
            case ShortInlineBrTargetOperand:
                from.Operand = new ShortInlineBrTargetOperand { Instruction = to };
                return;
            case InlineSwitchOperand:
                throw new NotImplementedException("Switch rewriting not implemented");
                return;
        }
    }
}
