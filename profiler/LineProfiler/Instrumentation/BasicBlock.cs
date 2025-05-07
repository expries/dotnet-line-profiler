using IlRewriter.Models;

namespace LineProfiler.Instrumentation;

public class BasicBlock(int startOffset, List<Instruction> instructions)
{
    public string Id => StartOffset.ToString();
    public int StartOffset { get; } = startOffset;
    public List<int> InstructionOffsets { get; } = instructions.Select(instruction => instruction.Offset).ToList();
    public List<Instruction> Instructions { get; } = instructions;
    public Instruction LastInstruction => Instructions[^1];
    public List<BasicBlock> Predecessors { get; } = [];

    public List<int> GetCoveredInstructionOffsets()
    {
        var offsets = GetCoveredInstructions()
            .Select(instruction => instruction.Offset)
            .OrderBy(offset => offset)
            .ToList();

        return offsets;
    }

    private List<Instruction> GetCoveredInstructions()
    {
        var instructions = Instructions.ToList();

        foreach (var block in Predecessors)
        {
            if (block.StartOffset == 0 || Predecessors.Count == 1)
            {
                var blockInstructions = block.GetCoveredInstructions();
                instructions.AddRange(blockInstructions);
            }
        }

        return instructions;
    }
}