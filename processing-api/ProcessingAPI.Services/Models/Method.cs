namespace ProcessingAPI.Services.Models;

public class Method
{
    public required string Id { get; set; }
    public int Revision { get; set; }
    public required string Name { get; set; }
    public required string FullTypeName { get; set; }
    public required string SourceCode { get; set; }
    public required string SourceFileName { get; set; }
    public long CodeStartLine { get; set; }
    public long CodeEndLine { get; set; }
    public List<MethodCall> Calls { get; set; } = [];
    public List<Branch> Branches { get; set; } = [];
}