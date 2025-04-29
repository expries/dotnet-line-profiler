using IlRewriter.Models;

namespace LineProfiler.Instrumentation;

public class BasicBlock
{
    public int StartOffset { get; }
    public List<Instruction> Instructions { get; }
    public Instruction LastInstruction => Instructions[^1];
    public List<BasicBlock> Predecessors { get; } = [];

    public BasicBlock(int startOffset, List<Instruction> instructions)
    {
        StartOffset = startOffset;
        Instructions = instructions;
    }
    
    public List<int> GetCoveredInstructionOffsets()
    {
        var instructions = GetCoveredInstructions();
        var offsets = instructions.Select(instruction => instruction.Offset);
        return offsets.ToList();
    }

    public List<Instruction> GetCoveredInstructions()
    {
        var instructions = Instructions;
        var block = this;

        while (block.Predecessors.Count == 1)
        {
            block = block.Predecessors[0];
            instructions.AddRange(block.Instructions);
        }

        return instructions;
    }
}