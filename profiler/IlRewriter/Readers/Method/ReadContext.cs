using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class ReadContext
{
    public MethodHeader Header { get; set; } = new();
    public Dictionary<int, Instruction> InstructionsByOffset { get; set; } = new();
    public Dictionary<int, Instruction> InstructionsByEndOffset { get; set; } = new();
}