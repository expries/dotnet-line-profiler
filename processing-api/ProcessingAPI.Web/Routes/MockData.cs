using ProcessingAPI.Web.Dtos;

namespace ProcessingAPI.Web.Routes;

public static class MockData
{
    public static MethodDto Method1 = new MethodDto
    {
        Name = "CallTrace",
        FullTypeName = "Program.Test"
    };

    public static MethodDto Method2 = new MethodDto
    {
        Name = "UseTrace",
        FullTypeName = "Program.Test"
    };
}