namespace IlRewriter.Models.Operands;

public class InlineSwitchOperand : Operand
{
    public int[] BranchOffsets { get; set; } = [];
    public Instruction[] Branches { get; set; } = [];
    public override int SizeInBytes => 4 + Branches.Length * 4;
}