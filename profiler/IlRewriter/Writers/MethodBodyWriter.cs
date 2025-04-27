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
        var instructionOffset = 0;

        foreach (var instruction in body.Instructions)
        {
            instruction.Offset = instructionOffset;
            instructionOffset += instruction.SizeInBytes;
        }
        
        CorrectBranchingOffsets(body);
        
        MethodPrinter.PrintMethodBody(body);
        
        var startPosition = stream.Position;

        foreach (var instruction in body.Instructions)
        {
            _instructionWriter.WriteToStream(instruction, stream);
        }

        var instructionEndPosition = stream.Position;
        
        _dataSectionWriter.WriteToStream(body.DataSection, stream);

        return instructionEndPosition - startPosition;
    }
    
    private static void CorrectBranchingOffsets(MethodBody body)
    {
        foreach (var instruction in body.Instructions)
        {
            switch (instruction.Operand)
            {
                case InlineBrTargetOperand operand:
                    if (operand.Instruction is null) continue;
                    operand.Value = operand.Instruction.Offset - instruction.EndOffset;
                    continue;
                case ShortInlineBrTargetOperand operand:
                    if (operand.Instruction is null) continue;
                    var offset = operand.Instruction.Offset - instruction.EndOffset;
                    
                    try
                    {
                        operand.Value = Convert.ToSByte(offset);
                    }
                    catch (OverflowException)
                    {
                        instruction.OpCode = OpCodes.Br;
                        instruction.Operand = new InlineBrTargetOperand
                        {
                            Instruction = instruction, 
                            Offset = operand.Offset, 
                            Value = offset
                        };
                    }

                    continue;
            }
        }
    }
}
