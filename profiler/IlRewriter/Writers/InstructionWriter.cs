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

        if (instruction.Operand is not null) WriteOperand(instruction, stream);
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

    private static void WriteOperand(Instruction instruction, Stream stream)
    {
        switch (instruction.Operand)
        {
            case ByteOperand byteOperand:
                new ByteOperandWriter().WriteToStream(byteOperand, stream);
                break;
            case DoubleOperand doubleOperand:
                new DoubleOperandWriter().WriteToStream(doubleOperand, stream);
                break;
            case InlineBrTargetOperand inlineBrTargetOperand:
                new InlineBrTargetOperandWriter().WriteToStream(inlineBrTargetOperand, stream);
                break;
            case ShortInlineBrTargetOperand shortInlineBrTargetOperand:
                new ShortInlineBrTargetOperandWriter().WriteToStream(shortInlineBrTargetOperand, stream);
                break;
            case InlineSwitchOperand inlineSwitchOperand:
                new InlineSwitchOperandWriter().WriteToStream(inlineSwitchOperand, instruction, stream);
                break;
            case Int16Operand int16Operand:
                new Int16OperandWriter().WriteToStream(int16Operand, stream);
                break;
            case Int32Operand int32Operand:
                new Int32OperandWriter().WriteToStream(int32Operand, stream);
                break;
            case Int64Operand int64Operand:
                new Int64OperandWriter().WriteToStream(int64Operand, stream);
                break;
            case SByteOperand sByteOperand:
                new SByteOperandWriter().WriteToStream(sByteOperand, stream);
                break;
            case ShortInlineIOperand shortInlineIOperand:
                new ShortInlineIOperandWriter().WriteToStream(shortInlineIOperand, stream);
                break;
            case SingleOperand singleOperand:
                new SingleOperandWriter().WriteToStream(singleOperand, stream);
                break;
            case UInt16Operand uInt16Operand:
                new UInt16OperandWriter().WriteToStream(uInt16Operand, stream);
                break;
            case UInt32Operand uInt32Operand:
                new UInt32OperandWriter().WriteToStream(uInt32Operand, stream);
                break;
            case UInt64Operand uInt64Operand:
                new UInt64OperandWriter().WriteToStream(uInt64Operand, stream);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}