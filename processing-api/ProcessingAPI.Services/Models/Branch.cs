namespace ProcessingAPI.Services.Models;

public class Branch
{
    public required string Id { get; set; }
    public required string BlockId { get; set; }
    public Method Method { get; set; }
    public int Executions { get; set; }
    public required int[] CodeLines { get; set; }
    public DateTime? LastTested { get; set; }
}