using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;

namespace IlRewriter.Models;

public class Instruction
{
    public int Offset { get; set; }
    public OpCode OpCode { get; set; }
    public Operand? Operand { get; set; } 
    
    public int SizeInBytes
    {
        get
        {
            var opCodeSize = OpCode.SizeInBytes;
            var operandSize = Operand?.SizeInBytes ?? 0;
            return opCodeSize + operandSize;
        }
    }
}