using IlRewriter.Models.Operands;

namespace IlRewriter.Writers.OperandWriters;

public class InlineSwitchOperandWriter
{
    public void WriteToStream(InlineSwitchOperand instruction, int switchInstructionOffset, Stream stream)
    {
        PrepareBranchOffsets(instruction, switchInstructionOffset);
        
        var lengthBytes = BitConverter.GetBytes(instruction.Branches.Length);
        stream.Write(lengthBytes);

        // TODO: to prepare in loop
        foreach (var branchOffset in instruction.BranchOffsets)
        {
            var offsetBytes = BitConverter.GetBytes(branchOffset);
            stream.Write(offsetBytes);
        }
    }

    private static void PrepareBranchOffsets(InlineSwitchOperand instruction, int switchInstructionOffset)
    {
        instruction.BranchOffsets = new int[instruction.Branches.Length];
        for (var i = 0; i < instruction.Branches.Length; i++)
        {
            var branchInstruction = instruction.Branches[i];
            var branchOffset = branchInstruction.Offset - switchInstructionOffset;
            Console.WriteLine($"switch branch {i}: {branchOffset} (switch offset: {switchInstructionOffset})");
            instruction.BranchOffsets[i] = branchOffset;
        }
    }
}