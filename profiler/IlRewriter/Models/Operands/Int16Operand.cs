namespace IlRewriter.Models.Operands;

public class Int16Operand : Operand
{
    public short Value { get; set; }
    public override int SizeInBytes => 2;
}