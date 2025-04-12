using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using OtelLineProfiler.Interception;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public static class Instrumentation
{
    private record BranchRange(int StartIndex, int EndIndex);
    
    internal record Branch
    {
        public Instruction Start { get; set; }
        public Instruction End { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
    
    internal class ProbeMarker
    {
        public Branch? Branch { get; set; }
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
        var probes = CollectProbes(method);
        
        PrintMethodWithProbes(method, probes);
        
        var localVarIndex = localVarCount - 1;
        AddProbes(method, boolTypeRef, localVarIndex, localSignature, probeSignature, probes);
        return method;
    }
    
    private static Dictionary<int, ProbeMarker> CollectProbes(Method method, BranchRange? branch = null)
    {
        // Default to method base branch
        branch ??= new BranchRange(StartIndex:0, EndIndex:method.Body.Instructions.Count);
        var probes = new Dictionary<int, ProbeMarker>();
        var instructionsInBranch = new List<int>();

        for (var i = branch.StartIndex; i < branch.EndIndex; i++)
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
            if (instruction.OpCode == OpCodes.Ret)
            {
                probes[i] = new ProbeMarker
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    Type = ProbeMarker.ProbeType.Return
                };
                break;
            }

            // end of current branch
            if (i == branch.EndIndex - 1)
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
            if (targetInstructionIndex == branch.EndIndex)
            {
                probes[branchingInstructionIndex - 1] = new ProbeMarker
                {
                    Type = ProbeMarker.ProbeType.End,
                    CoveredInstructionIndices = instructionsInBranch, 
                    Branch = new Branch(),
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
        }

        return false;
    }


    private static int CollectProbesInBranch(Method method, int branchingInstructionIndex, int targetInstructionIndex, out Dictionary<int, ProbeMarker> probesInBranch)
    {
        var branchStart = branchingInstructionIndex + 1;
        var branch = new BranchRange(StartIndex:branchStart, EndIndex:targetInstructionIndex);
        
        probesInBranch = CollectProbes(method, branch);
        probesInBranch = SetBranchForProbes(probesInBranch, new Branch());
        
        if (branch.EndIndex == branch.StartIndex) return branch.EndIndex;
        return branch.EndIndex - 1;
    }

    private static Dictionary<int, ProbeMarker> SetBranchForProbes(Dictionary<int, ProbeMarker> probes, Branch branch)
    {
        probes.ToList().ForEach(kv =>
        {
            var index = kv.Key;
            var probe = kv.Value;
            probes[index] = new ProbeMarker
            {
                Type = probe.Type,
                CoveredInstructionIndices = probe.CoveredInstructionIndices,
                Branch = branch
            };
        });

        return probes;
    }
    
    
    private static void AddProbes(Method method, MdTypeRef boolTypeRef, uint localVarIndex, MdSignature localSignature, MdSignature probeSignature, Dictionary<int, ProbeMarker> probes)
    {
        // TODO: Point branch instructions that point to a return to the start of the probe instructions
        var instructionOffset = AddInitialization(method, boolTypeRef, probes.Count, localVarIndex);
        var probeIndices = probes.Keys.ToList();
        
        for (var i = 0; i < probes.Count; i++)
        {
            var probeIndex = probeIndices[i];
            var probe = probes[probeIndex];
                
            switch (probe.Type)
            {
                case ProbeMarker.ProbeType.End:
                    instructionOffset += AddProbeForBranchEnd(
                        method, i,  probeIndex, localVarIndex, instructionOffset);
                    break;
                case ProbeMarker.ProbeType.Return:
                    instructionOffset += AddProbeForReturnStatement(
                        method, i, probeIndex, probes.Count, localVarIndex, instructionOffset, probeSignature);
                    break;
            }
        }

        CorrectBranchingOffsets(method);

        const int instrumentationStackSize = 3;
        method.Header.MaxStack = (ushort) (method.Header.MaxStack + instrumentationStackSize);
        method.Header.LocalVarSigToken = (uint) localSignature.Value;
    }

    private static int AddInitialization(Method method, MdTypeRef boolTypeRef, int arrayLength, uint localVarIndex)
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
        int i,
        int probeIndex, 
        uint localVarIndex, 
        int instructionOffset)
    {
        var instructions = new InstructionBuilder()
            .WriteTrueToArray(i, localVarIndex)
            .Build();
        
        var probeInstructionIndex = probeIndex + 1 + instructionOffset;
        method.Body.Instructions.InsertRange(probeInstructionIndex, instructions);
        
        Console.WriteLine($"p: {probeInstructionIndex} (length: {instructions.Count})");
        return instructions.Count;
    }

    private static unsafe int AddProbeForReturnStatement(
        Method method, 
        int i, 
        int probeIndex, 
        int probeCount, 
        uint localVarIndex, 
        int instructionOffset, 
        MdSignature probeSignature)
    {
        var probePointer = new UIntPtr(Callbacks.ProbePointer);
            
        var callbackProbe = new InstructionBuilder()
            .WriteTrueToArray(i, localVarIndex)
            .AddMethodCall(probePointer, probeSignature, probeCount, localVarIndex)
            .Build();

        var startOfProbe = callbackProbe[0];

        if (probe.Branch is not null)
        {
            var target = method.Body.Instructions[probe.Branch.TargetIndex + instructionOffset];
            Console.WriteLine("probe-index: " + probeIndex);
            Console.WriteLine("branch-to-index: " + probe.Branch.TargetIndex);

            if (target.OpCode == OpCodes.Ret)
            {
                Console.WriteLine("Correcting return at idx=" + probeIndex);
                ChangeTarget(probe.Branch.From, startOfProbe);
                var fromIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == probe.Branch.From.Offset);
                method.Body.Instructions[fromIdx] = probe.Branch.From;
            }

        }
                        
        var beforeReturnIndex = probeIndex + addedInstructionCount;
        Console.WriteLine($"p: {beforeReturnIndex} (length: {callbackProbe.Count})");
        method.Body.Instructions.InsertRange(beforeReturnIndex, callbackProbe);
        addedInstructionCount += callbackProbe.Count;
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
                    sTarget.Value = (sbyte) sTargetOffset;
                    continue;
            }
        }
    }
    
    private static int? GetTargetOffset(int i, Method method, Instruction? target)
    {
        if (target is null)
        {
            Console.WriteLine("No target found :(");
            return null;
        };
        
        var offset = 0;
        
        for (var j = i  + 1; j < method.Body.Instructions.Count; j++)
        {
            var instruction = method.Body.Instructions[j];
            
            Console.WriteLine(
                $"j={j}({offset} + {instruction.SizeInBytes} = {offset + instruction.SizeInBytes})");
            
            offset += instruction.SizeInBytes;

            if (target == instruction)
            {
                Console.WriteLine("found at j=" + j);
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
            Console.WriteLine($"[{i}]: {instruction.Offset}: {instruction.OpCode.Name}");

            if (probes.TryGetValue(i, out var probe))
            {
                Console.WriteLine(" -> p: " + probe.Type );
            }
        }

        Console.WriteLine("");
        Console.WriteLine("");
    }
}

