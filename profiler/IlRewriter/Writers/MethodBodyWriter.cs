using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;

namespace IlRewriter.Writers;

public class MethodBodyWriter
{
    private readonly IlWriter<Instruction> _instructionWriter = new InstructionWriter();
    private readonly IlWriter<MethodDataSection> _dataSectionWriter = new MethodDataSectionWriter();
    
    public long WriteToStream(MethodBody body, Stream stream)
    {
        var instructions = PopulateInstructionOffsets(body.Instructions);
        CorrectBranchingOffsets(instructions);
        instructions = PopulateInstructionOffsets(instructions);
        
        var startPosition = stream.Position;

        foreach (var instruction in instructions)
        {
            _instructionWriter.WriteToStream(instruction, stream);
        }

        var instructionEndPosition = stream.Position;
        
        _dataSectionWriter.WriteToStream(body.DataSection, stream);

        return instructionEndPosition - startPosition;
    }
    
    private static void CorrectBranchingOffsets(List<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            switch (instruction.Operand)
            {
                case InlineBrTargetOperand operand:
                    if (operand.Instruction is null) continue;
                    operand.Value = operand.Instruction.Offset - instruction.EndOffset;
                    break;
                case ShortInlineBrTargetOperand operand:
                    if (operand.Instruction is null) return;
                    var offset = operand.Instruction.Offset - instruction.EndOffset;
                    CorrectBranchingOffset(instruction, operand, offset);
                    break;
            }
        }
    }

    private static void CorrectBranchingOffset(
        Instruction instruction, 
        ShortInlineBrTargetOperand operand, 
        int offset)
    {
                    
        try
        {
            operand.Value = Convert.ToSByte(offset);
        }
        catch (OverflowException)
        {
            instruction.OpCode = OpCodes.Br;
            instruction.Operand = new InlineBrTargetOperand
            {
                Instruction = operand.Instruction, 
                Value = offset
            };
        }
    }

    private static List<Instruction> PopulateInstructionOffsets(List<Instruction> instructions)
    {
        var offset = 0;
        
        foreach (var instruction in instructions)
        {
            instruction.Offset = offset;
            offset += instruction.SizeInBytes;
        }
        
        return instructions;
    }
}
