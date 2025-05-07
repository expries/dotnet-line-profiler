namespace LineProfiler.DebugInfo;

public class PdbSourceCodeFile(string name, string[] lines)
{
    public string Name { get; set; } = name;
    public string[] Lines { get; set; } = lines;
}