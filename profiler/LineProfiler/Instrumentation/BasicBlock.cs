using IlRewriter.Models;

namespace LineProfiler.Instrumentation;

public class BasicBlock
{
    public BasicBlock(int startOffset, List<Instruction> instructions)
    {
        StartOffset = startOffset;
        Instructions = instructions;
    }

    public int StartOffset { get; }
    public List<Instruction> Instructions { get; }
    public Instruction LastInstruction => Instructions[^1];
    public List<BasicBlock> Predecessors { get; } = [];

    public List<int> GetCoveredInstructionOffsets()
    {
        var instructions = GetCoveredInstructions();
        var offsets = instructions
            .Select(instruction => instruction.Offset)
            .OrderBy(offset => offset)
            .ToList();

        return offsets;
    }

    private List<Instruction> GetCoveredInstructions()
    {
        var instructions = Instructions.ToList();

        foreach (var block in Predecessors)
            if (block.StartOffset == 0 || Predecessors.Count == 1)
            {
                var blockInstructions = block.GetCoveredInstructions();
                instructions.AddRange(blockInstructions);
            }

        return instructions;
    }
}