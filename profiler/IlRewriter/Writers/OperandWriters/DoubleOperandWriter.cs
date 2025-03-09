using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class DoubleOperandWriter : IlWriter<DoubleOperand>
{
    public override void WriteToStream(DoubleOperand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}