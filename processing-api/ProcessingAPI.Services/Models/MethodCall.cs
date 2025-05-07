namespace ProcessingAPI.Services.Models;

public class MethodCall
{
    public string Id { get; set; }
    public Method Method { get; set; }
    public List<Branch> Branches { get; set; }
    public required int[] ExecutedCodeLines { get; set; }
    public Span Span { get; set; }
}