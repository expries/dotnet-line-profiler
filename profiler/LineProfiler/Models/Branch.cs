using LineProfiler.DebugInfo;

namespace LineProfiler.Models;

public record Branch(string SourcePath, List<SourceLine> SourceLines)
{
    public string SourcePath { get; set; } = SourcePath;
    public List<SourceLine> SourceLines { get; set; } = SourceLines;
}