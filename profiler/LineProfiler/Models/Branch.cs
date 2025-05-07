namespace LineProfiler.Models;

public record Branch(
    string SourceName, 
    int[] SourceLines, 
    string BlockId, 
    int[] SourceLinesInBlock)
{
    public string SourceName { get; set; } = SourceName;
    public int[] SourceLines { get; set; } = SourceLines;
    public string BlockId { get; set; } = BlockId;
    public int[] SourceLinesInBlock { get; set; } = SourceLinesInBlock;
}