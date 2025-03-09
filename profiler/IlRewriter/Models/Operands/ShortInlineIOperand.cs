namespace IlRewriter.Models.Operands;

public class ShortInlineIOperand : Operand
{
    public int Value { get; set; }
    public override int SizeInBytes => 1;
}