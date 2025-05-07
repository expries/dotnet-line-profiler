using IlRewriter.Models;
using LineProfiler.Instrumentation;

namespace LineProfiler.Models;

public class Probe(BasicBlock block)
{
    public List<BranchToRewrite> BranchesToRewrite { get; set; } = [];
    public List<int> CoveredInstructionOffsets { get; set; } = [];
    public ProbeType Type { get; set; }
    public BasicBlock Block { get; set; } = block;

    public class BranchToRewrite(Instruction instruction, int? switchIndex = null)
    {
        public Instruction Instruction { get; set; } = instruction;
        public int? SwitchIndex { get; set; } = switchIndex;
    }
}