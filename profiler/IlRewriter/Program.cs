using IlRewriter.Models;
using IlRewriter.Readers;
using IlRewriter.Readers.Method;

const string currentDirectory = @"C:\Users\Samuel\RiderProjects\CodeLevelTracing\ILRewriter.Tests\";
var methodBodiesDirectory = Path.Combine(currentDirectory, "Assets", "ILMethodBodies");
var allDirectory = Path.Combine(methodBodiesDirectory, "All");
var withExceptionHandlersDirectory = Path.Combine(methodBodiesDirectory, "WithExceptionHandlers");

for (var i = 0; i < 45; i++)
{
    var fileName = $"method_body_{i}.il";
    var methodBodyIlPath = Path.Combine(allDirectory, fileName);
    var ilStream = File.OpenRead(methodBodyIlPath);

    var reader = new MethodReader(ilStream);
    var method = reader.ReadFromStream();

    foreach (var exceptionHandler in method.Body.DataSection.EhSections.SelectMany(ehSection => ehSection.Handlers))
    {
        Console.WriteLine($"HandlerType: {exceptionHandler.HandlerType}");
        Console.WriteLine($"TryStart: {exceptionHandler.TryStart.Offset}: {exceptionHandler.TryStart.OpCode.Name}");
        Console.WriteLine($"TryEnd: {exceptionHandler.TryEnd.Offset}: {exceptionHandler.TryStart.OpCode.Name}");
        Console.WriteLine($"HandlerStart: {exceptionHandler.HandlerStart.Offset}: {exceptionHandler.HandlerStart.OpCode.Name}");
        Console.WriteLine($"HandlerEnd: {exceptionHandler.HandlerEnd.Offset}: {exceptionHandler.HandlerEnd.OpCode.Name}");
            
        if (exceptionHandler.FilterStart is not null)
        {
            Console.WriteLine($"FilterStart: {exceptionHandler.FilterStart.Offset}: {exceptionHandler.FilterStart.OpCode.Name}");
        }
        if (exceptionHandler.CatchTypeToken is not null)
        {
            Console.WriteLine($"CatchType (Token): {exceptionHandler.CatchTypeToken}");
        }

        Console.WriteLine();
    }
}
