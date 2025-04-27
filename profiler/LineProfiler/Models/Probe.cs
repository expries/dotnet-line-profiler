using IlRewriter.Models;

namespace LineProfiler.Models;

public class Probe
{
    public List<Instruction> BranchesToRewrite { get; set; } = [];
    public List<int> CoveredInstructionOffsets { get; set; } = [];
    public ProbeType Type { get; set; }
}