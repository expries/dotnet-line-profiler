using SharpPdb.Managed;

namespace LineProfiler.DebugInfo;

public static class PdbParser
{
    private static readonly Dictionary<string, string[]> SourceCodeLinesByPath = new();

    public static PdbModule ParsePdbModule(string pdbPath)
    {
        var pdb = PdbFileReader.OpenPdb(pdbPath);
        var module = new PdbModule();

        pdb.Functions.ToList().ForEach(function =>
        {
            var parsedFunction = ParsePdbFunction(function);
            module.AddFunction(function.Token, parsedFunction);
        });

        return module;
    }

    private static PdbFunction ParsePdbFunction(IPdbFunction function)
    {
        var sequencePoints = function.SequencePoints.ToList();
        var parsedFunction = new PdbFunction();

        sequencePoints.ForEach(sequencePoint =>
        {
            var sequence = ParsePdbSequence(sequencePoint);
            parsedFunction.AddSequence(sequencePoint.Offset, sequence);
        });

        return parsedFunction;
    }

    private static PdbSequence ParsePdbSequence(IPdbSequencePoint sequencePoint)
    {
        var lineStartIdx = sequencePoint.StartLine;
        var lineEndIdx = Math.Max(sequencePoint.EndLine, sequencePoint.StartLine + 1);

        var sourceCodeFilePath = sequencePoint.Source.Name;
        var sourceCode = ReadSourceCodeFromFile(sourceCodeFilePath);
        var sequence = new PdbSequence(sequencePoint.Source.Name);


        for (var lineNumber = lineStartIdx; lineNumber < lineEndIdx; lineNumber++)
        {
            if (lineNumber < 0 || lineNumber >= sourceCode.Length) continue;
            var text = sourceCode[lineNumber - 1];
            var sourceCodeLine = new SourceLine(lineNumber, text);
            sequence.Lines.Add(sourceCodeLine);
        }

        return sequence;
    }

    private static string[] ReadSourceCodeFromFile(string sourcePath)
    {
        SourceCodeLinesByPath.TryGetValue(sourcePath, out var sourceCode);

        if (sourceCode is null)
        {
            sourceCode = File.ReadAllLines(sourcePath);
            SourceCodeLinesByPath.Add(sourcePath, sourceCode);
        }

        return sourceCode;
    }
}