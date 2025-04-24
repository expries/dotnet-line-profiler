using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Caches;

public static class ProbeCache
{
    private static readonly Dictionary<(AssemblyId, FunctionId), Dictionary<int, Probe>> Cache = new();

    public static bool TryGetProbes(
        AssemblyId assemblyId, 
        FunctionId functionId, 
        out Dictionary<int, Probe>? probes)
    {
        return Cache.TryGetValue((assemblyId, functionId), out probes);
    }
    
    public static Dictionary<int, Probe> AddMethodProbes( 
        AssemblyId assemblyId,
        FunctionId functionId, 
        Method method)
    {
        var probes = GetProbesInternal(method, 0, method.Body.Instructions.Count);
        Cache[(assemblyId, functionId)] = probes;
        return probes;
    }
    
    private static Dictionary<int, Probe> GetProbesInternal(Method method, int startIndex, int endIndex)
    {
        // Default to method base branch
        var probes = new Dictionary<int, Probe>();
        var instructionsInBranch = new List<int>();
        var instructionOffsetsInBranch = new List<int>();

        for (var i = startIndex; i < endIndex; i++)
        {
            var instruction = method.Body.Instructions[i];
            
            // branching (jumps/if/if-else,switch,etc.)
            var isBranchingInstruction = TryParseBranchingInstruction(method, instruction, out var targetIdx);

            if (isBranchingInstruction)
            {
                i = HandleBranch(i, targetIdx);
                continue;
            }
            
            instructionsInBranch.Add(i);
            instructionOffsetsInBranch.Add(instruction.Offset);

            // return of current branch
            if (instruction.OpCode == OpCodes.Ret)
            {
                probes[i] = new Probe
                {
                    CoveredInstructionIndices = instructionsInBranch,
                    CoveredInstructionOffsets = instructionOffsetsInBranch,
                    Type = ProbeType.Return
                };
                break;
            }

            // end of current branch
            if (i == endIndex - 1)
            {
                probes[i] = new Probe
                {
                    CoveredInstructionIndices = instructionsInBranch, 
                    CoveredInstructionOffsets = instructionOffsetsInBranch,
                    Type = ProbeType.End
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
                    Type = ProbeType.End,
                    CoveredInstructionIndices = instructionsInBranch, 
                    CoveredInstructionOffsets = instructionOffsetsInBranch,
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

        probesInBranch = GetProbesInternal(method, branchStart, targetInstructionIndex);
        
        // Check if jump target is a return statement or a branch's end
        var targetHasProbe = probesInBranch.TryGetValue(targetInstructionIndex, out var targetProbe);

        if (targetHasProbe && targetProbe is not null)
        {
            targetProbe.BranchToRewrite = new Branch { InstructionIndex = branchingInstructionIndex};
        }
        
        if (targetInstructionIndex == branchStart) return targetInstructionIndex;
        return targetInstructionIndex - 1;
    }
    
}