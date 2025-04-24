using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using IlRewriter.Models.Signatures;
using LineProfiler.Caches;
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

        CorrectBranchingOffsets(method);

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
        RewriteBranchIfSet(method, probeInstructions, probe);
        
        var probeInstructionIndex = probeIndex + 1 + instructionOffset;
        method.Body.Instructions.InsertRange(probeInstructionIndex, probeInstructions);
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
        RewriteBranchIfSet(method, probeInstructions, probe);
        
        var beforeReturnIndex = probeIndex + instructionOffset;
        method.Body.Instructions.InsertRange(beforeReturnIndex, probeInstructions);
        return probeInstructions.Count;
    }

    private static void RewriteBranchIfSet(Method method, List<Instruction> probeInstructions, Probe probe)
    {
        if (probe.BranchToRewrite is null) return;
        
        var branchingInstructionIndex = probe.BranchToRewrite.InstructionIndex;
        var branchingInstruction = method.Body.Instructions[branchingInstructionIndex];
        var startOfProbe = probeInstructions[0];
        ChangeTarget(branchingInstruction, startOfProbe);
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
        }
    }
    
    private static void CorrectBranchingOffsets(Method method)
    {
        for (var i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instruction = method.Body.Instructions[i];
                
            switch (instruction.Operand)
            {
                case InlineBrTargetOperand target:
                    var targetOffset = GetTargetOffset(i, method, target.Instruction) ?? 0;
                    target.Value = targetOffset;
                    continue;
                case ShortInlineBrTargetOperand sTarget:
                    var sTargetOffset = GetTargetOffset(i, method, sTarget.Instruction) ?? 0;

                    try
                    {
                        sTarget.Value = Convert.ToSByte(sTargetOffset);
                    }
                    catch (OverflowException)
                    {
                        instruction.OpCode = OpCodes.Br;
                        instruction.Operand = new InlineBrTargetOperand
                        {
                            Instruction = instruction, 
                            Offset = sTarget.Offset, 
                            Value = sTargetOffset
                        };
                    }

                    continue;
            }
        }
    }
    
    private static int? GetTargetOffset(int startIndex, Method method, Instruction? target)
    {
        if (target is null)
        {
            return null;
        }
        
        var offset = 0;
        var branchingOffset = -1;
        var targetOffset = -1;
        
        for (var i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instruction = method.Body.Instructions[i];
            
            offset += instruction.SizeInBytes;

            if (i == startIndex)
            {
                branchingOffset = offset;
            }

            if (target == instruction)
            {
                targetOffset = offset;
            }

            if (branchingOffset > -1 && targetOffset > -1)
            {
                return targetOffset - branchingOffset;
            }
        }
        
        return null;
    }
}
