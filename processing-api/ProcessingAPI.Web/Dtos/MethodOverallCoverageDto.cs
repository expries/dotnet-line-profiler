namespace ProcessingAPI.Web.Dtos;

public class MethodOverallCoverageDto
{
    public MethodDto Method { get; set; }
    public List<LineCoverage> Lines {get; set;}
    
    public class LineCoverage
    {
        public int LineNumber {get; set;}
        public string LineText {get; set;}
        public double ExecutedRelative {get; set;}
        public bool Tested {get; set;}
    }
}
