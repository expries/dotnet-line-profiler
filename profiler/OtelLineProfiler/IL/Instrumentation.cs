using System.Formats.Tar;
using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using OtelLineProfiler.Interception;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public static class Instrumentation
{
    internal class Branch
    {
        public Instruction From { get; set; }
        public int ToIndex { get; set; }
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
    
    public static unsafe Method InstrumentMethod(Method method, uint localVarCount, MdTypeRef boolTypeRef, MdSignature localSignature, MdSignature probeSignature)
    {
        var arrayPtrLocalVarIndex = localVarCount - 1;
        var probes = CollectProbes(method, 0, method.Body.Instructions.Count, null);

        Console.WriteLine("Probes:");
        foreach (var (probeIndex, probe) in probes)
        {
            var probeOffset = method.Body.Instructions[probeIndex].Offset;
            Console.WriteLine($"{probeOffset}: {probe.Type} [{string.Join(',', probe.CoveredInstructionIndices)}]");
        }

        Console.WriteLine("");
        
        AddProbes(method, boolTypeRef, arrayPtrLocalVarIndex, localSignature, probeSignature, probes);
        
        return method;
    }

    private static Dictionary<int, ProbeMarker> CollectProbes(Method method, int startIndex, int endIndex, Instruction? branchingInstruction)
    {
        var probes = new Dictionary<int, ProbeMarker>();
        var coveredInstructions = new List<int>();

        for (var i = startIndex; i < endIndex; i++)
        {
            var instruction = method.Body.Instructions[i];
            
            // TODO: Point branch instructions that point to a return to the start of the probe instructions
            switch (instruction.Operand)
            {
                case InlineBrTargetOperand target:
                    var targetIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == target.Offset);
                    target.Instruction = method.Body.Instructions[targetIdx];
                    Console.WriteLine("target start: " + target.Instruction?.OpCode.Name);
                    i = CollectProbesInBranch(i, target.Offset);
                    continue;
                case ShortInlineBrTargetOperand sTarget:
                    var sTargetIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == sTarget.Offset);
                    sTarget.Instruction = method.Body.Instructions[sTargetIdx];
                    Console.WriteLine("target start: " + sTarget.Instruction?.OpCode.Name);
                    i = CollectProbesInBranch(i, sTarget.Offset);
                    continue;
            }
            
            coveredInstructions.Add(i);

            // return of current branch
            if (instruction.OpCode == OpCodes.Ret)
            {
                Console.WriteLine("ret at offset=" + instruction.Offset);
                probes[i] = new ProbeMarker {  CoveredInstructionIndices = coveredInstructions, Type = ProbeMarker.ProbeType.Return };
                break;
            }

            // end of current branch
            if (i == endIndex - 1)
            {
                Console.WriteLine("end at offset=" + instruction.Offset);
                probes[i] = new ProbeMarker {  CoveredInstructionIndices = coveredInstructions, Type = ProbeMarker.ProbeType.End };
                break;
            }
        }

        return probes;

        int CollectProbesInBranch(
            int branchingInstructionIndex, 
            int targetOffset)
        {
            var branchInstruction = method.Body.Instructions[branchingInstructionIndex];
            var targetIdx = method.Body.Instructions.FindIndex(instruction => instruction.Offset == targetOffset);

            Console.WriteLine("target: " + targetIdx);
            Console.WriteLine("target instruction: " + method.Body.Instructions[targetIdx].OpCode.Name);
            
            // jump to end of current branch
            if (targetIdx == endIndex)
            {
                probes[branchingInstructionIndex - 1] = new ProbeMarker
                {
                    Type = ProbeMarker.ProbeType.End,
                    CoveredInstructionIndices = coveredInstructions, 
                    Branch = new Branch
                    {
                        From = branchInstruction,
                        ToIndex = targetIdx
                    }, 
                };
            }

            CollectProbes(method, branchingInstructionIndex + 1, targetIdx, branchInstruction)
                .ToList()
                .ForEach(kv =>
                {
                    var index = kv.Key;
                    var probe = kv.Value;
                    probes[index] = new ProbeMarker
                    {
                        Type = probe.Type,
                        CoveredInstructionIndices = probe.CoveredInstructionIndices,
                        Branch = new Branch
                        {
                            From = branchInstruction,
                            ToIndex = targetIdx,
                        }, 
                    };
                });
                    
            if (targetIdx == branchingInstructionIndex) return targetIdx;
            return targetIdx - 1;
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
        }
    }
    
    private static void AddProbes(Method method, MdTypeRef boolTypeRef, uint localVarIndex, MdSignature localSignature, MdSignature probeSignature, Dictionary<int, ProbeMarker> probes)
    {
        unsafe
        {
            var arrayLength = probes.Count;
            var probePointer = new UIntPtr(Callbacks.ProbePointer);
            
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];
                if (probes.TryGetValue(i, out var probe))
                {
                }

                if (probe?.Type is ProbeMarker.ProbeType.Return)
                {
                    Console.WriteLine(" -> p: " + probe.Type );
                }
                Console.WriteLine($"[{i}]: {instruction.Offset}: {instruction.OpCode.Name}");
                if (probe?.Type is ProbeMarker.ProbeType.End)
                {
                    Console.WriteLine(" -> p: " + probe.Type );
                }                
            }

            Console.WriteLine("");
            Console.WriteLine("");

            var initArray = new InstructionBuilder()
                .CreateArray(boolTypeRef, arrayLength, localVarIndex)
                .Build();
            
            method.Body.Instructions.InsertRange(0, initArray);
            var addedInstructionCount = initArray.Count;

            Console.WriteLine($"p: 0 (length: {addedInstructionCount})");

            for (var i = 0; i < probes.Count; i++)
            {
                var probeIndices = probes.Keys.ToList();
                var probeIndex = probeIndices[i];
                var probe = probes[probeIndex];
                
                switch (probe.Type)
                {
                    case ProbeMarker.ProbeType.End:
                        var coverageProbe = new InstructionBuilder()
                            .WriteTrueToArray(i, localVarIndex)
                            .Build();
                        var coverageIndex = probeIndex + addedInstructionCount + 1;
                        Console.WriteLine($"p: {coverageIndex} (length: {coverageProbe.Count})");
                        method.Body.Instructions.InsertRange(coverageIndex, coverageProbe);
                        addedInstructionCount += coverageProbe.Count;
                        break;
                    case ProbeMarker.ProbeType.Return:
                        var callbackProbe = new InstructionBuilder()
                            .WriteTrueToArray(i, localVarIndex)
                            .AddMethodCall(probePointer, probeSignature, probes.Count, localVarIndex)
                            .Build();

                        var startOfProbe = callbackProbe[0];

                        if (probe.Branch is not null)
                        {
                            Console.WriteLine("probe-index: " + probeIndex);
                            Console.WriteLine("branch-to-index: " + probe.Branch.ToIndex);
                        }

                        if (probe.Branch is not null && probe.Branch.ToIndex == probeIndex)
                        {
                            Console.WriteLine("Correcting return at idx=" + probeIndex);
                            var fromIdx = method.Body.Instructions.FindIndex(instr => instr.Offset == probe.Branch.From.Offset);
                            ChangeTarget(probe.Branch.From, startOfProbe);
                            method.Body.Instructions[fromIdx] = probe.Branch.From;
                        }
                        
                        var callbackIndex = probeIndex + addedInstructionCount;
                        Console.WriteLine($"p: {callbackIndex} (length: {callbackProbe.Count})");
                        method.Body.Instructions.InsertRange(callbackIndex, callbackProbe);
                        addedInstructionCount += callbackProbe.Count;
                        break;
                }
            }

            
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];
                
                switch (instruction.Operand)
                {
                    case InlineBrTargetOperand target:
                        Console.WriteLine("target before: " + target.Instruction?.OpCode.Name);
                        target.Value = GetTargetOffset(i, method, target.Instruction) ?? 5;
                        Console.WriteLine("target: " + target.Offset + "(" + target.Instruction?.OpCode.Name + ")" + ", value=" + target.Value);
                        continue;
                    case ShortInlineBrTargetOperand sTarget:
                        Console.WriteLine("target before: " + sTarget.Instruction.OpCode.Name);
                        sTarget.Value = (sbyte) (GetTargetOffset(i, method, sTarget.Instruction) ?? 5);
                        Console.WriteLine("target: " + sTarget.Offset + "(" + sTarget.Instruction.OpCode.Name + ")" + ", value=" + sTarget.Value);
                        continue;
                }
            }
            
            method.Header.MaxStack = (ushort) (method.Header.MaxStack + 3);
            method.Header.LocalVarSigToken = (uint) localSignature.Value;
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
}

