using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;

namespace LineProfiler.Instrumentation;

public class ControlFlowGraph
{
    private readonly Dictionary<int, BasicBlock> _blockByInstructionOffset;
    private readonly Dictionary<int, (int Index, Instruction Instruction)> _instructionByOffset;

    public ControlFlowGraph(List<Instruction> instructions)
    {
        Instructions = instructions;
        _instructionByOffset = BuildInstructionLookup();
        Blocks = GetBlocks();
        _blockByInstructionOffset = BuildBlockLookup();

        PopulatePredecessors();
    }

    public List<BasicBlock> Blocks { get; set; }
    public List<Instruction> Instructions { get; set; }

    private void PrintGraph()
    {
        PrintBlocks();
        PrintBlockLinks();
    }

    private void PrintBlocks()
    {
        Console.WriteLine("Blocks:");
        foreach (var block in Blocks)
        {
            var offsets = block.Instructions.Select(i => i.Offset).ToArray();
            var formattedOffsets = string.Join(' ', offsets);
            Console.WriteLine($"{block.StartOffset}: {formattedOffsets}");
        }

        Console.WriteLine();
    }

    private void PrintBlockLinks()
    {
        Console.WriteLine("Block links:");
        foreach (var block in Blocks)
        {
            var predecessorOffsets = block.Predecessors.Select(p => p.StartOffset).ToArray();
            var formattedPredecessors = string.Join(' ', predecessorOffsets);
            Console.WriteLine($"{block.StartOffset}: {formattedPredecessors}");
        }

        Console.WriteLine();
    }

    private Dictionary<int, (int Index, Instruction Instruction)> BuildInstructionLookup()
    {
        return Instructions
            .Select((instruction, i) => (Index: i, Instruction: instruction))
            .ToDictionary(x => x.Instruction.Offset, x => (x.Index, x.Instruction));
    }

    private Dictionary<int, BasicBlock> BuildBlockLookup()
    {
        var blockByInstructionOffset = new Dictionary<int, BasicBlock>();
        foreach (var block in Blocks)
        foreach (var instruction in block.Instructions)
            blockByInstructionOffset[instruction.Offset] = block;

        return blockByInstructionOffset;
    }

    private List<BasicBlock> GetBlocks()
    {
        var count = Instructions.Count;

        // 1) Find all block-start indices
        var boundaries = new HashSet<int> { 0 };
        for (var i = 0; i < count; i++)
        {
            var instruction = Instructions[i];

            // a) any branch target
            foreach (var target in GetBranchTargets(instruction)) boundaries.Add(target.Index);

            // b) any instruction immediately after a branch or return
            if (instruction.IsJump ||
                instruction.OpCode == OpCodes.Ret ||
                instruction.OpCode == OpCodes.Switch)
                if (i + 1 < count)
                    boundaries.Add(i + 1);
        }

        // 2) Sort them to know each block’s start and end
        var sortedStarts = boundaries.OrderBy(index => index).ToList();
        var blocks = new List<BasicBlock>();

        for (var b = 0; b < sortedStarts.Count; b++)
        {
            var start = sortedStarts[b];
            var end = b + 1 < sortedStarts.Count ? sortedStarts[b + 1] : count;

            var blockOffset = Instructions[start].Offset;
            var blockInstructions = Instructions.Skip(start).Take(end - start).ToList();

            var block = new BasicBlock(blockOffset, blockInstructions);
            blocks.Add(block);
        }

        return blocks;
    }

    private void PopulatePredecessors()
    {
        var successorsByBlock = new Dictionary<BasicBlock, List<BasicBlock>>();

        for (var i = 0; i < Blocks.Count; i++)
        {
            var block = Blocks[i];
            var successors = new List<BasicBlock>();
            var branchTargets = GetBranchTargets(block.LastInstruction);

            // Branches;
            foreach (var target in branchTargets)
            {
                var targetBlock = _blockByInstructionOffset[target.Instruction.Offset];
                successors.Add(targetBlock);
            }

            // Last block
            if (i + 1 >= Blocks.Count)
            {
                successorsByBlock[block] = successors;
                continue;
            }

            var nextBlock = Blocks[i + 1];

            // Fall through:
            if (block.LastInstruction.OpCode != OpCodes.Ret && !block.LastInstruction.IsUnconditionalJump)
                successors.Add(nextBlock);

            successorsByBlock[block] = successors;
        }

        foreach (var (block, successors) in successorsByBlock)
        foreach (var successor in successors)
            successor.Predecessors.Add(block);
    }

    /// <summary>
    ///     Returns all true branch targets for this instruction, with any jump→jump chains collapsed.
    /// </summary>
    private List<(int Index, Instruction Instruction)> GetBranchTargets(Instruction instruction)
    {
        return instruction.Operand switch
        {
            InlineBrTargetOperand ib => [GetInstructionByOffset(instruction.EndOffset + ib.Value)],
            ShortInlineBrTargetOperand sb => [GetInstructionByOffset(instruction.EndOffset + sb.Value)],
            InlineSwitchOperand sw => ResolveSwitchTargets(instruction, sw),
            _ => []
        };
    }

    private List<(int Index, Instruction Instruction)> ResolveSwitchTargets(Instruction instruction,
        InlineSwitchOperand operand)
    {
        var targets = new List<(int, Instruction)>();

        for (var i = 0; i < operand.BranchOffsets.Length; i++)
        {
            var relativeOffset = operand.BranchOffsets[i];
            var target = GetInstructionByOffset(instruction.EndOffset + relativeOffset);
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