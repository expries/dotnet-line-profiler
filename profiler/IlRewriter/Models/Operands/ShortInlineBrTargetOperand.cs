namespace IlRewriter.Models.Operands;

public class ShortInlineBrTargetOperand : Operand
{
    public Instruction? Instruction { get; set; }
    public sbyte Value { get; set; }
    public int Offset { get; set; }
    public override int SizeInBytes => 1;
}