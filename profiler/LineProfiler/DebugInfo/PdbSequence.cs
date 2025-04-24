namespace LineProfiler.DebugInfo;

public class PdbSequence(string sourcePath)
{
    public string SourcePath { get; set; } = sourcePath;
    public List<SourceLine> Lines { get; set; } = [];
}