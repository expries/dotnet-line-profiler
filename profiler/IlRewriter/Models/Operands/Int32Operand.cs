namespace IlRewriter.Models.Operands;

public class Int32Operand : Operand
{
    public int Value { get; set; }
    public override int SizeInBytes => 4;
}