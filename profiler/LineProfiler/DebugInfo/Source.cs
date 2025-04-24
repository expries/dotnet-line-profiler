namespace LineProfiler.DebugInfo;

public class Source(string sourceFilePath, List<SourceLine> lines)
{
    public string SourceFilePath { get; set; } = sourceFilePath;
    public List<SourceLine> Lines { get; set; } = lines;
}