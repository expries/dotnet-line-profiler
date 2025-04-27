using IlRewriter.Models;
using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class InlineSwitchOperandWriter
{
    public void WriteToStream(InlineSwitchOperand instruction, Instruction switchInstruction, Stream stream)
    {
        var lengthBytes = BitConverter.GetBytes(instruction.Branches.Length);
        stream.Write(lengthBytes);

        for (var i = 0; i < instruction.Branches.Length; i++)
        {
            var branchInstruction = instruction.Branches[i];
            var relativeOffset = branchInstruction.Offset - switchInstruction.EndOffset;
            instruction.BranchOffsets[i] = relativeOffset;
            var offsetBytes = BitConverter.GetBytes(relativeOffset);
            stream.Write(offsetBytes);
        }
    }
}