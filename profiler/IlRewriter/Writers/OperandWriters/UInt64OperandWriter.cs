using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class UInt64OperandWriter : IlWriter<UInt64Operand>
{
    public override void WriteToStream(UInt64Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}