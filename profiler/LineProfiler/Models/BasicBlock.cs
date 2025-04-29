using IlRewriter.Models;

namespace LineProfiler.Caches;

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
}