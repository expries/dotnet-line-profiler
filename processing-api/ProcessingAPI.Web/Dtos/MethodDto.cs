namespace ProcessingAPI.Web.Dtos;

public class MethodDto
{
    public string Id { get; set; }
    public string DisplayName => $"{FullTypeName}.{Name}";
    
    public string Name { get; set; }
    
    public string FullTypeName { get; set; }
}