namespace IlRewriter.Models.Operands;

public class SingleOperand : Operand
{
    public float Value  { get; set; }
    public override int SizeInBytes => 4;
}