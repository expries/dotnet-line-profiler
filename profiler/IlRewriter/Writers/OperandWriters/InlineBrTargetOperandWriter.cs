using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class InlineBrTargetOperandWriter : IlWriter<InlineBrTargetOperand>
{
    public override void WriteToStream(InlineBrTargetOperand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}