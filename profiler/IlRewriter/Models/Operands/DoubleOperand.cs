namespace IlRewriter.Models.Operands;

public class DoubleOperand : Operand
{
    public double Value  { get; set; }
    public override int SizeInBytes => 1;
}