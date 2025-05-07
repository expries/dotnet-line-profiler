namespace ProcessingAPI.Web.Dtos;

public class MethodCallDto
{
    public string CallId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string TraceId { get; set; }
    
    public string SpanId { get; set; }
    
    public DateTime SpanStart { get; set; }
    
    public DateTime SpanEnd { get; set; }
    
    public MethodDto Method { get; set; }
}