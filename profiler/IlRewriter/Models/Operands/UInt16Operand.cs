namespace IlRewriter.Models.Operands;

public class UInt16Operand : Operand
{
    public ushort Value { get; set; }
    public override int SizeInBytes => 2;
}