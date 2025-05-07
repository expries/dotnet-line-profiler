using LineProfiler.Caches;
using SharpPdb.Managed;

namespace LineProfiler.DebugInfo;

public static class PdbParser
{

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
        var parsedFunction = new PdbFunction { Token = function.Token};

        sequencePoints.ForEach(sequencePoint =>
        {
            var sequence = ParsePdbSequence(sequencePoint, function);
            parsedFunction.AddSequence(sequencePoint.Offset, sequence);
        });

        return parsedFunction;
    }

    private static PdbSequence ParsePdbSequence(IPdbSequencePoint sequencePoint, IPdbFunction function)
    {
        var lineStartIdx = sequencePoint.StartLine;
        var lineEndIdx = Math.Max(sequencePoint.EndLine, sequencePoint.StartLine + 1);

        var sourceCodeFilePath = sequencePoint.Source.Name;
        var sourceCode = ReadSourceCodeFromFile(sourceCodeFilePath, cacheKey: function.Token);
        var sequence = new PdbSequence(sequencePoint.Source.Name);

        for (var lineNumber = lineStartIdx; lineNumber < lineEndIdx; lineNumber++)
        {
            if (lineNumber < 0 || lineNumber >= sourceCode.Length) continue;
            var text = sourceCode[lineNumber - 1];
            sequence.LineNumbers.Add(lineNumber);
        }

        return sequence;
    }

    private static string[] ReadSourceCodeFromFile(string sourcePath, int cacheKey)
    {
        if (CallbackCache.Instance.SourceFile.TryGetValue(cacheKey, out var sourceFile))
        {
            return sourceFile.Lines;
        }

        var sourceFileName = Path.GetFileName(sourcePath);
        var sourceCode = File.ReadAllLines(sourcePath);
        sourceFile = new PdbSourceCodeFile(sourceFileName, sourceCode);
        CallbackCache.Instance.SourceFile.Add(cacheKey, sourceFile);
        return sourceCode;
    }
}