using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class Int32OperandWriter : IlWriter<Int32Operand>
{
    public override void WriteToStream(Int32Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}