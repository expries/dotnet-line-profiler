using IlRewriter.Models;
using IlRewriter.Models.Operands;

namespace IlRewriter.Readers.Method;

public class MethodBodyReader : IlReader<MethodBody>
{
    private readonly IlReader<Instruction> _instructionReader;
    private readonly IlReader<MethodDataSection> _dataSectionReader;
	
    public MethodBodyReader(Stream stream, ReadContext context) : base(stream, context)
    {
        _instructionReader = new InstructionReader(stream, context);
        _dataSectionReader = new MethodDataSectionReader(stream, context);
    }

    public override MethodBody ReadFromStream()
    {
        var header = Context.Header;
        MoveTo((uint) header.SizeInBytes);
		
        while (Position < EndPosition)
        {
            var offset = Offset;
            var instruction = _instructionReader.ReadFromStream();
            instruction.Offset = offset;
            
            Context.InstructionsByOffset[offset] = instruction;
            Context.InstructionsByEndOffset[Offset] = instruction;
        }
		
        ResolveBranches();

        var dataSection = _dataSectionReader.ReadFromStream();
        
        return new MethodBody
        {
            Instructions = Context.InstructionsByOffset.Values.ToList(),
            DataSection = dataSection
        };
    }
    
    private void ResolveBranches()
    {
        foreach (var instruction in Context.InstructionsByOffset.Values)
        {
            switch (instruction.Operand)
            {
                case InlineSwitchOperand switchOperand:
                    ResolveSwitchBranches(instruction, switchOperand);
                    break;
                case ShortInlineBrTargetOperand shortInlineOperand:
                    shortInlineOperand.Instruction = 
                        Context.InstructionsByOffset[instruction.EndOffset + shortInlineOperand.Value];
                    break;
                case InlineBrTargetOperand inlineOperand:
                    inlineOperand.Instruction = 
                        Context.InstructionsByOffset[instruction.EndOffset + inlineOperand.Value];
                    break;
            }   
        }
    }

    private void ResolveSwitchBranches(Instruction switchInstruction, InlineSwitchOperand operand)
    {
        for (var i = 0; i < operand.BranchOffsets.Length; i++)
        {
            var instructionOffset = switchInstruction.EndOffset + operand.BranchOffsets[i];
            operand.Branches[i] = Context.InstructionsByOffset[instructionOffset];
        }
    }
}