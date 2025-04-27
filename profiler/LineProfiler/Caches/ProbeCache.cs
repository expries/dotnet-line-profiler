using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Caches;

public class BasicBlock
{
    public int Id { get; }
    public int StartOffset { get; }
    public List<Instruction> Instructions { get; }
    public Instruction LastInstruction => Instructions[^1];
    public List<BasicBlock> Predecessors { get; } = [];

    public BasicBlock(int id, int startOffset, List<Instruction> instructions)
    {
        Id = id;
        StartOffset = startOffset;
        Instructions = instructions;
    }
}

public class ControlFlowGraph
{
    public Dictionary<int, Probe> Probes { get; set; }
    private readonly List<Instruction> _instructions;
    private readonly Dictionary<int, (int Index, Instruction Instruction)> _instructionByOffset;
    private readonly List<BasicBlock> _blocks;
    private readonly Dictionary<int, BasicBlock> _blockByInstructionOffset;

    public ControlFlowGraph(List<Instruction> instructions)
    {
        _instructions = instructions;
        _instructionByOffset = BuildInstructionLookup();
        _blocks = GetBlocks();
        _blockByInstructionOffset = BuildBlockLookup();

        Probes = new Dictionary<int, Probe>();

        PopulatePredecessors();
        PopulateProbes();
        PrintGraph();
    }

    private void PrintGraph()
    {
        PrintBlocks();
        PrintBlockLinks();
    }

    private void PrintBlocks()
    {
        Console.WriteLine("Blocks:");
        foreach (var block in _blocks)
        {
            var offsets = block.Instructions.Select(i => i.Offset).ToArray();
            var formattedOffsets = string.Join(' ', offsets);
            Console.WriteLine($"{block.StartOffset}: {formattedOffsets}");
        }
    }

    private void PrintBlockLinks()
    {
        Console.WriteLine("Block links:");
        foreach (var block in _blocks)
        {
            var predecessorOffsets = block.Predecessors.Select(p => p.StartOffset).ToArray();
            var formattedPredecessors = string.Join(' ', predecessorOffsets);
            Console.WriteLine($"{block.StartOffset}: {formattedPredecessors}");
        }
    }

    private Dictionary<int, (int Index, Instruction Instruction)> BuildInstructionLookup()
    {
        return _instructions
            .Select((instruction, i) => (Index: i, Instruction: instruction))
            .ToDictionary(x => x.Instruction.Offset, x => (x.Index, x.Instruction));
    }

    private Dictionary<int, BasicBlock> BuildBlockLookup()
    {
        var blockByInstructionOffset = new Dictionary<int, BasicBlock>();
        foreach (var block in _blocks)
        foreach (var instruction in block.Instructions)
            blockByInstructionOffset[instruction.Offset] = block;
        return blockByInstructionOffset;
    }

    private List<BasicBlock> GetBlocks()
    {
        var count = _instructions.Count;

        // 1) Find all block-start indices
        var boundaries = new HashSet<int> { 0 };
        for (var i = 0; i < count; i++)
        {
            var instruction = _instructions[i];

            // a) any branch target
            foreach (var target in GetBranchTargets(instruction))
            {
                boundaries.Add(target.Index);
            }

            // b) any instruction immediately after a branch or return
            if (instruction.IsJump ||
                instruction.OpCode == OpCodes.Ret ||
                instruction.OpCode == OpCodes.Switch)
            {
                if (i + 1 < count)
                {
                    boundaries.Add(i + 1);
                }
            }
        }

        // 2) Sort them to know each block’s start and end
        var sortedStarts = boundaries.OrderBy(index => index).ToList();
        var blocks = new List<BasicBlock>();

        for (var b = 0; b < sortedStarts.Count; b++)
        {
            var start = sortedStarts[b];
            var end = b + 1 < sortedStarts.Count ? sortedStarts[b + 1] : count;

            var blockId = blocks.Count;
            var blockOffset = _instructions[start].Offset;
            var blockInstructions = _instructions.Skip(start).Take(end - start).ToList();

            var block = new BasicBlock(blockId, blockOffset, blockInstructions);
            blocks.Add(block);
        }

        return blocks;
    }

    private void PopulatePredecessors()
    {
        var successorsByBlock = new Dictionary<BasicBlock, List<BasicBlock>>();

        for (var i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i];
            var successors = new List<BasicBlock>();
            var branchTargets = GetBranchTargets(block.LastInstruction);
            
            // Branches
            foreach (var target in branchTargets)
            {
                var targetBlock = _blockByInstructionOffset[target.Instruction.Offset];
                successors.Add(targetBlock);
            }

            // Last block
            if (i + 1 >= _blocks.Count)
            {
                successorsByBlock[block] = successors;
                continue;
            }

            var nextBlock = _blocks[i + 1];

            // Fall through:
            if (block.LastInstruction.OpCode != OpCodes.Ret && !block.LastInstruction.IsUnconditionalJump)
            {
                successors.Add(nextBlock);
            }

            successorsByBlock[block] = successors;
        }

        foreach (var (block, successors) in successorsByBlock)
        {
            foreach (var successor in successors)
            {
                successor.Predecessors.Add(block);
            }
        }
    }

    private void PopulateProbes()
    {
        Probes = new Dictionary<int, Probe>();
        foreach (var block in _blocks)
        {
            if (block.LastInstruction.OpCode == OpCodes.Ret)
            {
                var returnInstruction = GetInstructionByOffset(block.LastInstruction.Offset);
                var targetsToRewrite = GetJumpsTargetingLastInstruction(block);

                Probes[returnInstruction.Index] = new Probe
                {
                    Type = ProbeType.Return,
                    BranchesToRewrite = targetsToRewrite
                };
            }

            if (block.Predecessors.Count <= 1) continue;
            foreach (var predecessor in block.Predecessors)
            {
                var last = GetInstructionByOffset(predecessor.LastInstruction.Offset);
                var targetsToRewrite = GetJumpsTargetingLastInstruction(predecessor);

                if (last.Instruction.OpCode == OpCodes.Ret)
                {
                    Probes[last.Index] = new Probe
                    {
                        Type = ProbeType.Return,
                        BranchesToRewrite = targetsToRewrite
                    };
                }
                else
                {
                    Probes[last.Index] = new Probe
                    {
                        Type = ProbeType.End,
                        BranchesToRewrite = targetsToRewrite
                    };
                }
            }
        }
    }

    private static List<Instruction> GetJumpsTargetingLastInstruction(BasicBlock block)
    {
        if (block.Instructions.Count > 1)
        {
            return [];
        }
        
        return block.Predecessors
            .Select(p => p.LastInstruction)
            .Where(i => i.IsJump)
            .ToList();
    }

    /// <summary>
    /// Returns all true branch targets for this instruction, with any jump→jump chains collapsed.
    /// </summary>
    private List<(int Index, Instruction Instruction)> GetBranchTargets(Instruction instruction)
    {
        return instruction.Operand switch
        {
            InlineBrTargetOperand ib => [GetInstructionByOffset(instruction.EndOffset + ib.Value)],
            ShortInlineBrTargetOperand sb => [GetInstructionByOffset(instruction.EndOffset + sb.Value)],
            InlineSwitchOperand sw => ResolveSwitchTargets(sw),
            _ => []
        };
    }

    private List<(int Index, Instruction Instruction)> ResolveSwitchTargets(InlineSwitchOperand operand)
    {
        var targets = new List<(int, Instruction)>();

        for (var i = 0; i < operand.BranchOffsets.Length; i++)
        {
            var targetInstruction = operand.Branches[i];
            var target = GetInstructionByOffset(targetInstruction.Offset);
            targets.Add(target);
        }

        return targets;
    }

    private (int Index, Instruction Instruction) GetInstructionByOffset(int targetOffset)
    {
        var (index, instruction) = _instructionByOffset[targetOffset];
        return (Index: index, Instruction: instruction);
    }
}

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

    public static void AddMethodProbes(
        AssemblyId assemblyId,
        FunctionId functionId,
        Dictionary<int, Probe> probes)
    {
        Cache[(assemblyId, functionId)] = probes;
    }
}