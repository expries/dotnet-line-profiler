namespace IlRewriter.Models.Operands;

public class UInt64Operand : Operand
{
    public ulong Value { get; set; }
    public override int SizeInBytes => 8;
}