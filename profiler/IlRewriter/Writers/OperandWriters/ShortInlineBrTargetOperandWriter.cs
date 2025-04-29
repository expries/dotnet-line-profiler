using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class ShortInlineBrTargetOperandWriter : IlWriter<ShortInlineBrTargetOperand>
{
    public override void WriteToStream(ShortInlineBrTargetOperand instruction, Stream stream)
    {
        var signedBytes = new[] { instruction.Value };
        var bytes = (byte[])(Array)signedBytes;
        stream.Write(bytes);
    }
}