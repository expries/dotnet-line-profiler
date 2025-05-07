using LineProfiler.Instrumentation;
using LineProfiler.Models;
using SharpPdb.Managed.Portable;

namespace LineProfiler.DebugInfo;

public class PdbFunction
{
    public int Token { get; set; }
    public List<PdbSequence> SequencePoints => SequencesByOffset.Values.ToList();
    public readonly Dictionary<int, PdbSequence> SequencesByOffset = new();

    public void AddSequence(int instructionOffset, PdbSequence sequence)
    {
        SequencesByOffset[instructionOffset] = sequence;
    }

    public (List<int>, string) GetLinesByInstructionOffsets(int[] offsets)
    {
        var sourcePath = string.Empty;
        var sourceLines = new List<int>();
        
        foreach (var offset in offsets)
        {
            var sequenceFound = TryGetSequenceByOffset(offset, out var sequence);

            if (!sequenceFound || sequence is null) continue;

            sourcePath = sequence.SourcePath;
            sourceLines.AddRange(sequence.LineNumbers);
        }

        return (sourceLines, sourcePath);
    }
    
    private bool TryGetSequenceByOffset(int instructionOffset, out PdbSequence? sequence)
    {
        return SequencesByOffset.TryGetValue(instructionOffset, out sequence);
    }
}