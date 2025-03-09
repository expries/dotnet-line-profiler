using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class SByteOperandWriter : IlWriter<SByteOperand>
{
    public override void WriteToStream(SByteOperand instruction, Stream stream)
    {
        var signedBytes = new[] { instruction.Value };
        var bytes = (byte[]) (Array) signedBytes;
        stream.Write(bytes);
    }
}