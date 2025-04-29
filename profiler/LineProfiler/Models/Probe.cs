using IlRewriter.Models;

namespace LineProfiler.Models;

public class Probe
{
    public List<BranchToRewrite> BranchesToRewrite { get; set; } = [];
    public List<int> CoveredInstructionOffsets { get; set; } = [];
    public ProbeType Type { get; set; }

    public class BranchToRewrite(Instruction instruction, int? switchIndex = null)
    {
        public Instruction Instruction { get; set; } = instruction;
        public int? SwitchIndex { get; set; } = switchIndex;
    }
}