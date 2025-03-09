using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class InlineSwitchOperandWriter : IlWriter<InlineSwitchOperand>
{
    public override void WriteToStream(InlineSwitchOperand instruction, Stream stream)
    {
        var lengthBytes = BitConverter.GetBytes(instruction.Branches.Length);
        stream.Write(lengthBytes);

        foreach (var branchOffset in instruction.BranchOffsets)
        {
            var originalOffset = branchOffset - instruction.BaseOffset;
            var offsetBytes = BitConverter.GetBytes(originalOffset);
            stream.Write(offsetBytes);
        }
    }
}