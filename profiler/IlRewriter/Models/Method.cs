namespace IlRewriter.Models;

public class Method
{
    public MethodHeader Header { get; init; } = new();
    public MethodBody Body { get; init; } = new();
}