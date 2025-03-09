using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class SingleOperandWriter : IlWriter<SingleOperand>
{
    public override void WriteToStream(SingleOperand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}