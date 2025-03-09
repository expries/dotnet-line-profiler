using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class Int16OperandWriter : IlWriter<Int16Operand>
{
    public override void WriteToStream(Int16Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}