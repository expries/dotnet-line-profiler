namespace IlRewriter.Models.Operands;

public class SByteOperand : Operand
{
    public sbyte Value  { get; set; }
    public override int SizeInBytes => 1;
}