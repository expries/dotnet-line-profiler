using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Models;

namespace LineProfiler.Instrumentation;

public class ControlFlowGraphParser
{
    private readonly Dictionary<int, (int Index, Instruction Instruction)> _instructionsByOffset;
    private readonly ControlFlowGraph _graph;

    public ControlFlowGraphParser(ControlFlowGraph controlFlowGraph)
    {
        _graph = controlFlowGraph;
        _instructionsByOffset = BuildInstructionLookup();
    }

    private Dictionary<int, (int Index, Instruction Instruction)> BuildInstructionLookup()
    {
        return _graph.Instructions
            .Select((instruction, i) => (Index: i, Instruction: instruction))
            .ToDictionary(x => x.Instruction.Offset, x => (x.Index, x.Instruction));
    }

    public Dictionary<int, Probe> LocateProbes()
    {
        var probes = new Dictionary<int, Probe>();
        foreach (var block in _graph.Blocks)
        {
            if (block.LastInstruction.OpCode == OpCodes.Ret)
            {
                var returnInstruction = GetInstructionByOffset(block.LastInstruction.Offset);
                var targetsToRewrite = GetJumpsTargetingLastInstruction(block);

                probes[returnInstruction.Index] = new Probe
                {
                    Type = ProbeType.Return,
                    BranchesToRewrite = targetsToRewrite,
                    CoveredInstructionOffsets = block.GetCoveredInstructionOffsets()
                };
            }

            if (block.Predecessors.Count <= 1) continue;

            foreach (var predecessor in block.Predecessors)
            {
                if (predecessor.StartOffset == 0)
                {
                    continue;
                }

                var last = GetInstructionByOffset(predecessor.LastInstruction.Offset);
                var targetsToRewrite = GetJumpsTargetingLastInstruction(predecessor);
                var coveredOffsets = predecessor.GetCoveredInstructionOffsets();
                probes[last.Index] = new Probe
                {
                    Type = ProbeType.End,
                    BranchesToRewrite = targetsToRewrite,
                    CoveredInstructionOffsets = coveredOffsets
                };
            }
        }

        var orderedProbes = probes.OrderBy(p => p.Key).ToDictionary();
        return orderedProbes;
    }

    private (int Index, Instruction Instruction) GetInstructionByOffset(int targetOffset)
    {
        var (index, instruction) = _instructionsByOffset[targetOffset];
        return (Index: index, Instruction: instruction);
    }

    private static List<Probe.BranchToRewrite> GetJumpsTargetingLastInstruction(BasicBlock block)
    {
        if (block.Instructions.Count > 1)
        {
            return [];
        }

        var previousInstructions = block.Predecessors.Select(preBlock => preBlock.LastInstruction);
        var previousJumps = previousInstructions.Where(instruction => instruction.IsJump);

        return previousJumps.Select(instruction =>
            {
                if (instruction.Operand is not InlineSwitchOperand switchOperand)
                {
                    return new Probe.BranchToRewrite(instruction);
                }

                var switchIndex = switchOperand.Branches
                    .ToList()
                    .FindIndex(b => b.Offset == block.LastInstruction.Offset);

                return switchIndex < 0
                    ? new Probe.BranchToRewrite(instruction)
                    : new Probe.BranchToRewrite(instruction, switchIndex);
            })
            .ToList();
    }
}