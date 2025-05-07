namespace ProcessingAPI.Services.Models;

public class Span
{
    public required string Id { get; set; }
    public required string RootSpanId { get; set; }
    public required string TraceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}