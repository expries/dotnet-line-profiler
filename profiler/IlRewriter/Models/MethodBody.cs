namespace IlRewriter.Models;

public class MethodBody
{
    public List<Instruction> Instructions { get; set; } = [];
    public MethodDataSection DataSection { get; set; } = new();
}