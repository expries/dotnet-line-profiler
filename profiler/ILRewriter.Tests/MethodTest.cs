namespace ILRewriter.Tests;

public class MethodTest
{
    public static List<object[]> AllIlMethodBodies = ReadMethods("All");
    public static List<object[]> IlMethodBodiesWithExceptionHandlers = ReadMethods("WithExceptionHandlers");

    private static List<object[]> ReadMethods(string directoryName)
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "ILMethodBodies", directoryName);
        var files = Directory.GetFiles(directory);
        return files.Select(filePath =>
        {
            var bytes = File.ReadAllBytes(filePath);
            return bytes.Select(@byte => (object)@byte).ToArray();
        }).ToList();
    }
}