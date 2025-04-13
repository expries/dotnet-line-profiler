using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class InlineSwitchOperandWriter
{
    public void WriteToStream(InlineSwitchOperand instruction, int switchInstructionOffset, Stream stream)
    {
        var lengthBytes = BitConverter.GetBytes(instruction.Branches.Length);
        stream.Write(lengthBytes);

        for (var i = 0; i < instruction.BranchOffsets.Length; i++)
        {
            var branchInstruction = instruction.Branches[i];
            var branchOffset = branchInstruction.Offset - switchInstructionOffset;
            instruction.BranchOffsets[i] = branchOffset;
            var offsetBytes = BitConverter.GetBytes(branchOffset);
            stream.Write(offsetBytes);
        }
    }
}