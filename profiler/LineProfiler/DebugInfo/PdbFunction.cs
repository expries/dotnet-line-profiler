using LineProfiler.Models;

namespace LineProfiler.DebugInfo;

public class PdbFunction
{
    public readonly Dictionary<int, PdbSequence> SequencesByOffset = new();

    public void AddSequence(int instructionOffset, PdbSequence sequence)
    {
        SequencesByOffset[instructionOffset] = sequence;
    }
    
    public bool TryGetSequenceByOffset(int instructionOffset, out PdbSequence? sequence)
    {
        return SequencesByOffset.TryGetValue(instructionOffset, out sequence);
    }

    public Branch GetBranchByInstructionOffsets(List<int> offsets)
    {
        var branch = new Branch(string.Empty, []);
        
        foreach (var offset in offsets)
        {
            var sequenceFound = TryGetSequenceByOffset(offset, out var sequence);

            if (!sequenceFound || sequence is null) continue;

            branch.SourcePath = sequence.SourcePath;
            branch.SourceLines.AddRange(sequence.Lines);
        }

        return branch;
    }
    
    
}