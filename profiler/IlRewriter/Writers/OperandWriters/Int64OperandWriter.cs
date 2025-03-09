using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class Int64OperandWriter : IlWriter<Int64Operand>
{
    public override void WriteToStream(Int64Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}