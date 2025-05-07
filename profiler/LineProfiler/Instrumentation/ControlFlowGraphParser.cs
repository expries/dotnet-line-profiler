using IlRewriter.Models;
using IlRewriter.Models.Operands;
using LineProfiler.Models;

namespace LineProfiler.Instrumentation;

public class ControlFlowGraphParser
{
    private readonly ControlFlowGraph _graph;
    private readonly Dictionary<int, (int Index, Instruction Instruction)> _instructionsByOffset;

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
            LocateProbesInBlock(block, probes);
        }

        return probes
            .OrderBy(p => p.Key)
            .ToDictionary();
    }

    private void LocateProbesInBlock(BasicBlock block, Dictionary<int, Probe> probes)
    {
        if (block.LastInstruction.IsReturn)
        {
            var returnInstruction = GetInstructionByOffset(block.LastInstruction.Offset);
            var targetsToRewrite = GetJumpsTargetingLastInstruction(block);
            var coveredOffsets = block.GetCoveredInstructionOffsets();

            probes[returnInstruction.Index] = new Probe(block)
            {
                Type = ProbeType.Return,
                BranchesToRewrite = targetsToRewrite,
                CoveredInstructionOffsets = coveredOffsets,
            };
        }

        if (block.Predecessors.Count <= 1) return;

        foreach (var predecessor in block.Predecessors)
        {
            LocateProbesInPredecessor(predecessor, probes);
        }
    }

    private void LocateProbesInPredecessor(BasicBlock predecessor, Dictionary<int, Probe> probes)
    {
        if (predecessor.StartOffset == 0) return;

        var last = GetInstructionByOffset(predecessor.LastInstruction.Offset);
        var targetsToRewrite = GetJumpsTargetingLastInstruction(predecessor);
        var coveredOffsets = predecessor.GetCoveredInstructionOffsets();

        probes[last.Index] = new Probe(predecessor)
        {
            Type = ProbeType.End,
            BranchesToRewrite = targetsToRewrite,
            CoveredInstructionOffsets = coveredOffsets,
        };
    }

    private (int Index, Instruction Instruction) GetInstructionByOffset(int targetOffset)
    {
        var (index, instruction) = _instructionsByOffset[targetOffset];
        return (Index: index, Instruction: instruction);
    }

    private static List<Probe.BranchToRewrite> GetJumpsTargetingLastInstruction(BasicBlock block)
    {
        if (block.Instructions.Count > 1) return [];
        
        return block.Predecessors
            .Select(predecessor => predecessor.LastInstruction)
            .Where(instruction => instruction.IsJump)
            .Select(instruction => CreateBranchToRewrite(instruction, block.LastInstruction))
            .ToList();
    }

    private static Probe.BranchToRewrite CreateBranchToRewrite(Instruction from, Instruction to)
    {
        // Simple branch: rewrite one target
        if (from.Operand is not InlineSwitchOperand switchOperand)
        {
            return new Probe.BranchToRewrite(from);
        }

        // For switches: find branch of switch statement to rewrite
        var switchIndex = switchOperand.Branches
            .ToList()
            .FindIndex(instruction => instruction.Offset == to.Offset);

        if (switchIndex < 0)
        {
            return new Probe.BranchToRewrite(from);
        }

        return new Probe.BranchToRewrite(from, switchIndex);
    }
}