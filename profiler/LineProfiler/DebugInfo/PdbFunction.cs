namespace LineProfiler.DebugInfo;

public class PdbFunction
{
    public readonly Dictionary<int, PdbSequence> SequencesByOffset = new ();

    public bool TryGetSequenceByOffset(int instructionOffset, out PdbSequence? sequence)
    {
        return SequencesByOffset.TryGetValue(instructionOffset, out sequence);
    }

    public void AddSequence(int instructionOffset, PdbSequence sequence)
    {
        SequencesByOffset[instructionOffset] = sequence;
    }
}