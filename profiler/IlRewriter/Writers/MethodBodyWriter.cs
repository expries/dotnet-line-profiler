using IlRewriter.Models;

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
            instructionOffset += instruction.SizeInBytes;
            instruction.Offset = instructionOffset;
        }
        
        var startPosition = stream.Position;
        
        MethodPrinter.PrintMethodBody(body);


        foreach (var instruction in body.Instructions)
        {
            _instructionWriter.WriteToStream(instruction, stream);
        }

        var instructionEndPosition = stream.Position;
        
        _dataSectionWriter.WriteToStream(body.DataSection, stream);

        return instructionEndPosition - startPosition;
    }
}
