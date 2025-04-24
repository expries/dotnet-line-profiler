namespace LineProfiler.Models;

public class Probe
{
    public Branch? BranchToRewrite { get; set; }
    public List<int> CoveredInstructionIndices { get; set; } = [];
    public List<int> CoveredInstructionOffsets { get; set; } = [];
    public ProbeType Type { get; set; }
}