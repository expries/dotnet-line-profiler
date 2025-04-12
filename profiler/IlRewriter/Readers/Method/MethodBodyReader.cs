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
            var instruction = _instructionReader.ReadFromStream();
            instruction.Offset = Offset;
            Context.InstructionsByOffset[Offset] = instruction;
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
                    ResolveSwitchBranches(switchOperand);
                    break;
                case ShortInlineBrTargetOperand shortInlineOperand:
                    shortInlineOperand.Instruction = Context.InstructionsByOffset[Offset];
                    break;
                case InlineBrTargetOperand inlineOperand:
                    inlineOperand.Instruction = Context.InstructionsByOffset[Offset];
                    break;
            }   
        }
    }

    private void ResolveSwitchBranches(InlineSwitchOperand operand)
    {
        for (var j = 0; j < operand.BranchOffsets.Length; j++)
        {
            var instructionOffset = operand.BaseOffset + operand.BranchOffsets[j];
            operand.Branches[j] = Context.InstructionsByOffset[instructionOffset];
        }
    }
}