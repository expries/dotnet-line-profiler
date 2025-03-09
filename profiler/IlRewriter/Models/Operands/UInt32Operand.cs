namespace IlRewriter.Models.Operands;

public class UInt32Operand : Operand
{
    public uint Value  { get; set; }
    public override int SizeInBytes => 4;
}