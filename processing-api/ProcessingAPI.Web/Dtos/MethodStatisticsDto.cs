namespace ProcessingAPI.Web.Dtos;

public class MethodStatisticsDto
{
    public MethodDto Method { get; set; }
    
    public int TotalExecutions { get; set; }
    
    public double Hotness { get; set; }
}

