using IlRewriter;
using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Interception;
using Profiler.Core;

namespace LineProfiler.IL;

public record InstrumentationMetadata
{
    public AssemblyId AssemblyId { get; set; }
    public FunctionId FunctionId { get; set; }
    public MdTypeRef BoolType { get; set; }
    public MdMemberRef GetCurrentActivity { get; set; }
    public MdMemberRef GetTraceId { get; set; }
    public MdMemberRef TraceIdToHexString { get; set; }
    public MdSignature CallbackSignature { get; set; }
    public MdSignature LocalVarSignature { get; set; }
    public uint LocalVarCount { get; set; }
}

public static class Instrumentation
{
    internal class Probe
    {
        internal record Branch
        {
            public int InstructionIndex { get; set; }
        }
        public Branch? BranchTargetToRewrite { get; set; }
        public List<int> CoveredInstructionIndices { get; set; } = [];
        public ProbeType Type { get; set; }

        internal enum ProbeType
        {
            End,
            Return
        }
    }
    
    public static readonly byte[] ProbeSignature = MethodSignatureFactory.Create(
        CorUnmanagedCallingConventionFlags.IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL,
        // Number of arguments
        CorElementTypeFlags.ELEMENT_TYPE_VOID, 5,
        // Bool array pointer
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN,
        // Bool array length
        CorElementTypeFlags.ELEMENT_TYPE_I4,
        // Assembly id
        CorElementTypeFlags.ELEMENT_TYPE_I4,
        // Function id
        CorElementTypeFlags.ELEMENT_TYPE_I4, 
        // Trace id
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_CHAR);
    
    public static LocalVarSignature CreateLocalVarSignature(LocalVarSignature signature)
    {
        byte[] arraySignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_SZARRAY, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        byte[] arrayPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        byte[] traceIdPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_CHAR];
        signature.AddLocalVariable(arraySignature);
        signature.AddLocalVariable(arrayPtrSignature);
        signature.AddLocalVariable(traceIdPtrSignature);

        return signature;
    }
    
    public static Method InstrumentMethod(
        Method method, 
        InstrumentationMetadata metadata)
    {
        var probes = CollectProbes(method, 0, method.Body.Instructions.Count);
        AddProbes(method, probes, metadata);
        return method;
    }
    
    private static Dictionary<int, Probe> CollectProbes(Method method, int startIndex, int endIndex)
    {
        // Default to method base branch
        var probes = new Dictionary<int, Probe>();
        var instructionsInBranch = new List<int>();

        for (var i = startIndex; i < endIndex; i++)
        {
            var instruction = method.Body.Instructions[i];
            var isBranchingInstruction = TryParseBranchingInstruction(method, instruction, out var targetIdx);

            // branching (jumps/if/if-else,switch,etc.)
            if (isBranchingInstruction)
            {
                i = HandleBranch(i, targetIdx);
                continue;
            }
            
            instructionsInBranch.Add(i);

            // return of current branch
            var isReturnInstruction = instruction.OpCode == OpCodes.Ret;
            if (isReturnInstruction)
            {
                probes[i] = new Probe
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    Type = Probe.ProbeType.Return
                };
                break;
            }

            // end of current branch
            var isLastInstructionInBranch = i == endIndex - 1;
            if (isLastInstructionInBranch)
            {
                probes[i] = new Probe
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    Type = Probe.ProbeType.End
                };
                break;
            }
        }

        return probes;

        int HandleBranch(int branchingInstructionIndex, int targetInstructionIndex)
        {
            // treat loop as end of branch
            if (targetInstructionIndex < branchingInstructionIndex)
            {
                return branchingInstructionIndex;
            }
            
            // jump to end of current branch
            if (targetInstructionIndex == endIndex)
            {
                probes[branchingInstructionIndex - 1] = new Probe
                {
                    Type = Probe.ProbeType.End,
                    CoveredInstructionIndices = instructionsInBranch, 
                };
            }

            var i = CollectProbesInBranch(
                method,  branchingInstructionIndex, targetInstructionIndex, out var probesInBranch);
            
            probesInBranch.ToList().ForEach(kv => probes[kv.Key] = kv.Value);
            return i;
        }
    }
    
    private static bool TryParseBranchingInstruction(Method method, Instruction instruction, out int targetIdx)
    {
        targetIdx = -1;
        
        switch (instruction.Operand)
        {
            case InlineBrTargetOperand targetOperand:
                targetIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == targetOperand.Offset);
                targetOperand.Instruction = method.Body.Instructions[targetIdx];
                return true;
            case ShortInlineBrTargetOperand sTargetOperand:
                targetIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == sTargetOperand.Offset);
                sTargetOperand.Instruction = method.Body.Instructions[targetIdx];
                return true;
            default:
                return false;
        }
    }


    private static int CollectProbesInBranch(
        Method method, 
        int branchingInstructionIndex, 
        int targetInstructionIndex, 
        out Dictionary<int, Probe> probesInBranch)
    {
        // if branching instruction points to itself, ignore the branch
        if (branchingInstructionIndex == targetInstructionIndex)
        {
            probesInBranch = new Dictionary<int, Probe>();
            return branchingInstructionIndex;
        }
        
        var branchStart = branchingInstructionIndex + 1;

        probesInBranch = CollectProbes(method, branchStart, targetInstructionIndex);
        
        // Check if jump target is a return statement or a branch's end
        var targetHasProbe = probesInBranch.TryGetValue(targetInstructionIndex, out var targetProbe);

        if (targetHasProbe && targetProbe is not null)
        {
            targetProbe.BranchTargetToRewrite = new Probe.Branch { InstructionIndex = branchingInstructionIndex};
        }
        
        if (targetInstructionIndex == branchStart) return targetInstructionIndex;
        return targetInstructionIndex - 1;
    }
    
    
    private static void AddProbes(
        Method method,
        Dictionary<int, Probe> probes,
        InstrumentationMetadata metadata)
    {
        var instructionOffset = AddInitialization(method, metadata.BoolType, probes.Count, metadata.LocalVarCount);
        var probeIndices = probes.Keys.ToList();
        
        for (var i = 0; i < probes.Count; i++)
        {
            var probeIndex = probeIndices[i];
            var probe = probes[probeIndex];
            
            switch (probe.Type)
            {
                case Probe.ProbeType.End:
                    instructionOffset += AddProbeForBranchEnd(
                        method, probes,i,  probeIndex, metadata.LocalVarCount, instructionOffset);
                    break;
                case Probe.ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(
                        method, probes, i, probeIndex, metadata.LocalVarCount, instructionOffset, metadata);
                    break;
            }
        }

        CorrectBranchingOffsets(method);

        const int instrumentationStackSize = 10;
        method.Header.MaxStack = (ushort) (method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint) metadata.LocalVarSignature.Value;
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
                probes.Count, 
                10, 40)
            .Build();
        
        var probe = probes[probeIndex];
        RewriteBranchIfSet(method, probeInstructions, probe);
        
        var beforeReturnIndex = probeIndex + instructionOffset;
        method.Body.Instructions.InsertRange(beforeReturnIndex, probeInstructions);
        return probeInstructions.Count;
    }

    private static void RewriteBranchIfSet(Method method, List<Instruction> probeInstructions, Probe probe)
    {
        if (probe.BranchTargetToRewrite is null) return;
        
        var branchingInstructionIndex = probe.BranchTargetToRewrite.InstructionIndex;
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
