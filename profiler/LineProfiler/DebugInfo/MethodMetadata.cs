namespace LineProfiler.DebugInfo;

public class MethodMetadata(
    string name,
    string typeName,
    int startLine,
    int endLine)
{
    public string Name { get; set; } = name;
    public string TypeName { get; set; } = typeName;
    public int StartLine { get; set; } = startLine;
    public int EndLine { get; set; } = endLine;
}