namespace IlRewriter.Models.Operands;

public class Int64Operand : Operand
{
    public long Value  { get; set; }
    public override int SizeInBytes => 8;
}