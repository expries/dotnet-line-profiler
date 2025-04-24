namespace LineProfiler.DebugInfo;

public class SourceLine(int lineNumber, string text)
{
    public int LineNumber { get; set; } = lineNumber;
    public string Text { get; set; } = text;
}