using IlRewriter;
using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using IlRewriter.Writers;
using LineProfiler.Interception;
using Profiler.Core;

namespace LineProfiler.IL;

public static class Instrumentation
{
    private record InstructionRange(int StartIndex, int EndIndex);
    
    internal record Branch
    {
        public int InstructionIndex { get; set; }
    }
    
    internal class ProbeMarker
    {
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
        CorElementTypeFlags.ELEMENT_TYPE_VOID, 2, 
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN, 
        CorElementTypeFlags.ELEMENT_TYPE_I4);
    
    public static LocalVarSignature CreateLocalVarSignature(LocalVarSignature signature)
    {
        byte[] arraySignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_SZARRAY, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        byte[] arrayPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        signature.AddLocalVariable(arraySignature);
        signature.AddLocalVariable(arrayPtrSignature);
        return signature;
    }
    
    public static unsafe Method InstrumentMethod(
        Method method, 
        uint localVarCount, 
        MdTypeRef boolTypeRef, 
        MdSignature localSignature, 
        MdSignature probeSignature)
    {
        Console.WriteLine("Collecting probes ...");
        var probes = CollectProbes(method);

        Console.WriteLine("Method with probes ...");
        PrintMethodWithProbes(method, probes);
        
        var localVarIndex = localVarCount - 1;
        Console.WriteLine("Adding probes ...");
        AddProbes(method, probes, localVarIndex, boolTypeRef, localSignature, probeSignature);
        return method;
    }
    
    private static Dictionary<int, ProbeMarker> CollectProbes(Method method, InstructionRange? range = null)
    {
        // Default to method base branch
        range ??= new InstructionRange(StartIndex:0, EndIndex:method.Body.Instructions.Count);
        var probes = new Dictionary<int, ProbeMarker>();
        var instructionsInBranch = new List<int>();

        for (var i = range.StartIndex; i < range.EndIndex; i++)
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
                probes[i] = new ProbeMarker
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    Type = ProbeMarker.ProbeType.Return
                };
                break;
            }

            // end of current branch
            var isLastInstructionInBranch = i == range.EndIndex - 1;
            if (isLastInstructionInBranch)
            {
                probes[i] = new ProbeMarker
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    Type = ProbeMarker.ProbeType.End
                };
                break;
            }
        }

        return probes;

        int HandleBranch(int branchingInstructionIndex, int targetInstructionIndex)
        {
            // jump to end of current branch
            if (targetInstructionIndex == range.EndIndex)
            {
                probes[branchingInstructionIndex - 1] = new ProbeMarker
                {
                    Type = ProbeMarker.ProbeType.End,
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
        out Dictionary<int, ProbeMarker> probesInBranch)
    {
        // if branching instruction points to itself, ignore the branch
        if (branchingInstructionIndex == targetInstructionIndex)
        {
            Console.WriteLine("Branching == Target");
            probesInBranch = new Dictionary<int, ProbeMarker>();
            return branchingInstructionIndex;
        }
        
        var branchStart = branchingInstructionIndex + 1;
        var range = new InstructionRange(StartIndex:branchStart, EndIndex:targetInstructionIndex);

        Console.WriteLine($"Collecting probes in branch: ({range.StartIndex}, {range.EndIndex})");
        probesInBranch = CollectProbes(method, range);
        
        // Check if jump target is a return statement or a branch's end
        var targetHasProbe = probesInBranch.TryGetValue(targetInstructionIndex, out var targetProbe);

        if (targetHasProbe && targetProbe is not null)
        {
            targetProbe.BranchTargetToRewrite = new Branch { InstructionIndex = branchingInstructionIndex};
        }
        
        if (range.EndIndex == range.StartIndex) return range.EndIndex;
        return range.EndIndex - 1;
    }
    
    
    private static void AddProbes(
        Method method,
        Dictionary<int, ProbeMarker> probes,
        uint localVarIndex, 
        MdTypeRef boolTypeRef,
        MdSignature localSignature, 
        MdSignature probeSignature)
    {
        var instructionOffset = AddInitialization(method, probes.Count, localVarIndex, boolTypeRef);
        var probeIndices = probes.Keys.ToList();
        
        for (var i = 0; i < probes.Count; i++)
        {
            var probeIndex = probeIndices[i];
            var probe = probes[probeIndex];
            
            switch (probe.Type)
            {
                case ProbeMarker.ProbeType.End:
                    instructionOffset += AddProbeForBranchEnd(
                        method, probes,i,  probeIndex, localVarIndex, instructionOffset);
                    break;
                case ProbeMarker.ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(
                        method, probes, i, probeIndex, localVarIndex, instructionOffset, probeSignature);
                    break;
            }
        }

        CorrectBranchingOffsets(method);

        const int instrumentationStackSize = 3;
        method.Header.MaxStack = (ushort) (method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint) localSignature.Value;
    }

    private static int AddInitialization(Method method, int arrayLength, uint localVarIndex, MdTypeRef boolTypeRef)
    {
        var initArray = new InstructionBuilder()
            .CreateArray(boolTypeRef, arrayLength, localVarIndex)
            .Build();
            
        method.Body.Instructions.InsertRange(0, initArray);
        Console.WriteLine($"p: 0 (length: {initArray.Count})");
        return initArray.Count;
    }

    private static int AddProbeForBranchEnd(
        Method method,
        Dictionary<int, ProbeMarker> probes,
        int arrayIndex, 
        int probeIndex,
        uint localVarIndex, 
        int instructionOffset)
    {
        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, localVarIndex)
            .Build();
        
        var probe = probes[probeIndex];
        CorrectJumpToProbe(method, probeInstructions, probe);
        
        var probeInstructionIndex = probeIndex + 1 + instructionOffset;
        method.Body.Instructions.InsertRange(probeInstructionIndex, probeInstructions);
        
        Console.WriteLine($"p: {probeInstructionIndex} (length: {probeInstructions.Count})");
        return probeInstructions.Count;
    }
    
    private static unsafe int AddProbeForReturnStatement(
        Method method,
        Dictionary<int, ProbeMarker> probes,
        int arrayIndex,
        int probeIndex, 
        uint localVarIndex, 
        int instructionOffset, 
        MdSignature probeSignature)
    {
        var probePointer = new UIntPtr(Callbacks.ProbePointer);
            
        var probeInstructions = new InstructionBuilder()
            .WriteTrueToArray(arrayIndex, localVarIndex)
            .AddMethodCall(probePointer, probeSignature, probes.Count, localVarIndex)
            .Build();
        
        var probe = probes[probeIndex];
        CorrectJumpToProbe(method, probeInstructions, probe);
        
        var beforeReturnIndex = probeIndex + instructionOffset;
        method.Body.Instructions.InsertRange(beforeReturnIndex, probeInstructions);
        
        Console.WriteLine($"p: {beforeReturnIndex} (length: {probeInstructions.Count})");
        return probeInstructions.Count;
    }

    private static void CorrectJumpToProbe(Method method, List<Instruction> probeInstructions, ProbeMarker probe)
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
                        instruction.Operand = new InlineBrTargetOperand { Instruction = instruction, Offset = sTarget.Offset, Value = sTargetOffset };
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
        };
        
        var offset = 0;
        
        for (var j = startIndex  + 1; j < method.Body.Instructions.Count; j++)
        {
            var instruction = method.Body.Instructions[j];
            
            offset += instruction.SizeInBytes;

            if (target == instruction)
            {
                return offset;
            }
        }
        
        return null;
    }
    
    private static void PrintMethodWithProbes(Method method, Dictionary<int, ProbeMarker> probes)
    {
        for (var i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instruction = method.Body.Instructions[i];
            Console.Write($"[{i}]: {instruction.Offset}: {instruction.OpCode.Name}");
            
            if (instruction.Operand is not null)
            {
                MethodPrinter.PrintOperand(instruction.Operand);
            }

            Console.WriteLine();

            if (probes.TryGetValue(i, out var probe))
            {
                Console.WriteLine(" -> p: " + probe.Type );
            }
        }

        Console.WriteLine("");
        Console.WriteLine("");
    }
}

