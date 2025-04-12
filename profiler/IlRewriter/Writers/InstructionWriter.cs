using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.Operands;
using IlRewriter.Writers.OperandWriters;

namespace IlRewriter.Writers;

public class InstructionWriter : IlWriter<Instruction>
{
    public override void WriteToStream(Instruction instruction, Stream stream)
    {
        WriteOpCode(instruction.OpCode, stream);
        
        if (instruction.Operand is not null)
        {
            WriteOperand(instruction.Operand, instruction.Offset, stream);
        }
        
        Console.Write(Environment.NewLine);
    }
    
    private static void WriteOpCode(OpCode opCode, Stream stream)
    {
        if (opCode.Op1 == 0xff)
        {
            stream.WriteByte(opCode.Op2);
        }
        else
        {
            stream.WriteByte(opCode.Op1);
            stream.WriteByte(opCode.Op2);
        }
    }
    
    private static void WriteOperand(Operand operand, int instructionOffset, Stream stream)
    {
        switch (operand)
        {
            case ByteOperand byteOperand:
                Console.Write($" ({byteOperand.Value})");
                new ByteOperandWriter().WriteToStream(byteOperand, stream);
                break;
            case DoubleOperand doubleOperand:
                Console.Write($" ({doubleOperand.Value})");
                new DoubleOperandWriter().WriteToStream(doubleOperand, stream);
                break;
            case InlineBrTargetOperand inlineBrTargetOperand:
                Console.Write($" ({inlineBrTargetOperand.Value})");
                new InlineBrTargetOperandWriter().WriteToStream(inlineBrTargetOperand, stream);
                break;
            case InlineSwitchOperand inlineSwitchOperand:
                new InlineSwitchOperandWriter().WriteToStream(inlineSwitchOperand, instructionOffset, stream);
                break;
            case Int16Operand int16Operand:
                Console.Write($" ({int16Operand.Value})");
                new Int16OperandWriter().WriteToStream(int16Operand, stream);
                break;
            case Int32Operand int32Operand:
                Console.Write($" ({int32Operand.Value})");
                new Int32OperandWriter().WriteToStream(int32Operand, stream);
                break;
            case Int64Operand int64Operand:
                Console.Write($" ({int64Operand.Value})");
                new Int64OperandWriter().WriteToStream(int64Operand, stream);
                break;
            case SByteOperand sByteOperand:
                Console.Write($" ({sByteOperand.Value})");
                new SByteOperandWriter().WriteToStream(sByteOperand, stream);
                break;
            case ShortInlineBrTargetOperand shortInlineBrTargetOperand:
                Console.Write($" ({shortInlineBrTargetOperand.Value})");
                new ShortInlineBrTargetOperandWriter().WriteToStream(shortInlineBrTargetOperand, stream);
                break;
            case ShortInlineIOperand shortInlineIOperand:
                Console.Write($" ({shortInlineIOperand.Value})");
                new ShortInlineIOperandWriter().WriteToStream(shortInlineIOperand, stream);
                break;
            case SingleOperand singleOperand:
                Console.Write($" ({singleOperand.Value})");
                new SingleOperandWriter().WriteToStream(singleOperand, stream);
                break;
            case UInt16Operand uInt16Operand:
                Console.Write($" ({uInt16Operand.Value})");
                new UInt16OperandWriter().WriteToStream(uInt16Operand, stream);
                break;
            case UInt32Operand uInt32Operand:
                Console.Write($" ({uInt32Operand.Value})");
                new UInt32OperandWriter().WriteToStream(uInt32Operand, stream);
                break;
            case UInt64Operand uInt64Operand:
                Console.Write($" ({uInt64Operand.Value})");
                new UInt64OperandWriter().WriteToStream(uInt64Operand, stream);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}