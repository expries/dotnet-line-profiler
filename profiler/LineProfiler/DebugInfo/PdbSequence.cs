namespace LineProfiler.DebugInfo;

public class PdbSequence(string sourcePath)
{
    public string SourcePath { get; set; } = sourcePath;
    public List<int> LineNumbers { get; set; } = [];
}