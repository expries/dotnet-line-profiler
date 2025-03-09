using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class UInt16OperandWriter : IlWriter<UInt16Operand>
{
    public override void WriteToStream(UInt16Operand instruction, Stream stream)
    {
        var bytes = BitConverter.GetBytes(instruction.Value);
        stream.Write(bytes);
    }
}