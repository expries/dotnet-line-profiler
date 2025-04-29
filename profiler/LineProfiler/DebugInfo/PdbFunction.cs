namespace LineProfiler.DebugInfo;

public class PdbFunction
{
    public readonly Dictionary<int, PdbSequence> SequencesByOffset = new();

    public bool TryGetSequenceByOffset(int instructionOffset, out PdbSequence? sequence)
    {
        return SequencesByOffset.TryGetValue(instructionOffset, out sequence);
    }

    public void AddSequence(int instructionOffset, PdbSequence sequence)
    {
        SequencesByOffset[instructionOffset] = sequence;
    }

    public List<SourceLine> GetSourceLinesByOffsets(List<int> offsets)
    {
        var coveredLines = new List<SourceLine>();

        foreach (var offset in offsets)
        {
            var sequenceFound = TryGetSequenceByOffset(offset, out var sequence);

            if (!sequenceFound || sequence is null) continue;

            coveredLines.AddRange(sequence.Lines);
        }

        return coveredLines;
    }
}