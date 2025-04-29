using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class ShortInlineIOperandWriter : IlWriter<ShortInlineIOperand>
{
    public override void WriteToStream(ShortInlineIOperand instruction, Stream stream)
    {
        var bytes = new[] { (byte)instruction.Value };
        stream.Write(bytes);
    }
}