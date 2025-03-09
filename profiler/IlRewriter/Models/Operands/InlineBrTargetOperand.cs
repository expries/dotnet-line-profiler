namespace IlRewriter.Models.Operands;

public class InlineBrTargetOperand : Operand
{
    public Instruction? Instruction { get; set; }
    public int Value { get; set; }
    public int Offset { get; set; }
    public override int SizeInBytes => 4;
}