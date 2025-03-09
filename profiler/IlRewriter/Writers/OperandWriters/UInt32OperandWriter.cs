using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class UInt32OperandWriter : IlWriter<UInt32Operand>
{
    public override void WriteToStream(UInt32Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}