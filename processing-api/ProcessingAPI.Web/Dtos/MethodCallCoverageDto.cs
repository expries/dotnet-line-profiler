namespace ProcessingAPI.Web.Dtos;

public class MethodCallCoverageDto
{
    public string Id { get; set; }
    public MethodDto Method { get; set; }
    public List<LineCoverage> Lines {get; set;}
    
    public class LineCoverage
    {
        public int LineNumber {get; set;}
        public string LineText {get; set;}
        public bool Executed {get; set;}
    }
}

