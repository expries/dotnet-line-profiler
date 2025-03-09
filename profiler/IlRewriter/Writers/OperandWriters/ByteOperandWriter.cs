using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class ByteOperandWriter : IlWriter<ByteOperand>
{
    public override void WriteToStream(ByteOperand instruction, Stream stream)
    {
        var bytes = new[] { instruction.Value };
        stream.Write(bytes);
    }
}