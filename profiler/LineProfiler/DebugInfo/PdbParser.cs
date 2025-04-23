using SharpPdb.Managed;

namespace LineProfiler.DebugInfo;

public static class PdbParser
{
    private static readonly Dictionary<string, string[]> SourceCodeLinesByPath = new();
    
    public static Dictionary<int, Dictionary<int, List<SourceCodeLine>>> ParsePdbFunctions(string pdbPath)
    {
        var pdb = PdbFileReader.OpenPdb(pdbPath);
        var functions = new Dictionary<int, Dictionary<int, List<SourceCodeLine>>>();
        
        pdb.Functions.ToList().ForEach(function =>
        {
            functions[function.Token] = ParseSourceCodeLinesByOffset(function);
        });

        return functions;
    }

    private static Dictionary<int, List<SourceCodeLine>> ParseSourceCodeLinesByOffset(IPdbFunction function)
    {
        var linesByOffset = new Dictionary<int, List<SourceCodeLine>>();
        var sequencePoints= function.SequencePoints.ToList();
        
        sequencePoints.ForEach(sequencePoint =>
        {
            linesByOffset[sequencePoint.Offset] = ParseSourceCodeLines(sequencePoint);
        });
        
        return linesByOffset;
    }
    
    private static List<SourceCodeLine> ParseSourceCodeLines(IPdbSequencePoint sequencePoint)
    {
        var lineStartIdx = sequencePoint.StartLine;
        var lineEndIdx = Math.Max(sequencePoint.EndLine, sequencePoint.StartLine + 1);
        
        var sourceCodeFilePath = sequencePoint.Source.Name;
        var sourceCode  = GetSourceCodeLinesByPath(sourceCodeFilePath);
        var sourceCodeLines = new List<SourceCodeLine>();

        for (var lineNumber = lineStartIdx; lineNumber < lineEndIdx; lineNumber++)
        {
            if (lineNumber < 0 || lineNumber >= sourceCode.Length) continue;
            var text = sourceCode[lineNumber - 1];
            var sourceCodeLine = new SourceCodeLine(lineNumber, text);
            sourceCodeLines.Add(sourceCodeLine);
        }
        
        return sourceCodeLines;
    }
    
    private static string[] GetSourceCodeLinesByPath(string path)
    {
        SourceCodeLinesByPath.TryGetValue(path, out var sourceCode);

        if (sourceCode is null)
        {
            sourceCode = File.ReadAllLines(path);
            SourceCodeLinesByPath.Add(path, sourceCode);
        }
        
        return sourceCode;
    }

}