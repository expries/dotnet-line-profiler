namespace IlRewriter.Models.Operands;

public class ByteOperand : Operand
{
    public byte Value { get; set; }
    public override int SizeInBytes => 1;
}