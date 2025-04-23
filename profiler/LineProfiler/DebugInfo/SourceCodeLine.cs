namespace LineProfiler.DebugInfo;

public class SourceCodeLine(int lineNumber, string text)
{
    public int LineNumber { get; set; } = lineNumber;
    public string Text { get; set; } = text;
}