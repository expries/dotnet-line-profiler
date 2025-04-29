using IlRewriter.Models;
using IlRewriter.Models.OpCodes;
using IlRewriter.Models.OpCodes.Enums;
using IlRewriter.Models.Operands;

namespace IlRewriter.Readers.Method;

public class InstructionReader : IlReader<Instruction>
{
    public InstructionReader(Stream stream, ReadContext context) : base(stream, context)
    {
    }

    public override Instruction ReadFromStream()
    {
        var instruction = new Instruction
        {
            OpCode = ReadOpCode()
        };

        if (instruction.OpCode.OperandType != OperandType.InlineNone)
        {
            instruction.Operand = ReadOperand(instruction.OpCode);
        }
        
        return instruction;
    }

    private OpCode ReadOpCode()
    {
        var byteCode = ReadByte();
	    
        // Single byte opcodes
        if (byteCode != 0xfe)
        {
            return OpCodes.OneByteOpCode[byteCode];
        }

        // Two byte opcodes
        byteCode = ReadByte();
        return OpCodes.TwoBytesOpCode[byteCode];
    }

    private Operand ReadOperand(OpCode opCode)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineSwitch:
                return ReadSwitchOperand();
            case OperandType.ShortInlineBrTarget:
                return ReadShortInlineBrTargetOperand();
            case OperandType.InlineBrTarget:
                return ReadInlineBrTargetOperand();
            case OperandType.ShortInlineI:
                return ReadShortInlineIOperand(opCode);
            case OperandType.InlineI:
                return ReadInt32Operand();
            case OperandType.ShortInlineR:
                return ReadSingleOperand();
            case OperandType.InlineR:
                return ReadDoubleOperand();
            case OperandType.InlineI8:
                return ReadInt64Operand();
            case OperandType.ShortInlineVar:
            case OperandType.ShortInlineArg:
                return ReadByteOperand();
            case OperandType.InlineVar:
            case OperandType.InlineArg:
                return ReadUInt16Operand();
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.InlineMethod:
            case OperandType.InlineField:
                return ReadUInt32Operand();
            case OperandType.InlineNone:
            case OperandType.InlinePhi:
            default:
                throw new NotSupportedException();
        }
    }

    private InlineSwitchOperand ReadSwitchOperand()
    {
        var length = ReadUInt32();
        var branchOffsets = new int[length];
        
        for (var i = 0; i < length; i++)
        {
            var offset = ReadInt32();
            branchOffsets[i] = offset;
        }

        return new InlineSwitchOperand
        {
            BranchOffsets = branchOffsets,
            Branches = new Instruction[branchOffsets.Length],
        };
    }

    private ShortInlineBrTargetOperand ReadShortInlineBrTargetOperand()
    {
        var value = ReadSByte();
        var offset = value + Offset;
        return new ShortInlineBrTargetOperand
        {
            Offset = offset,
            Value = value,
        };
    }
    
    private ShortInlineIOperand ReadShortInlineIOperand(OpCode opCode)
    {
        int value = opCode == OpCodes.Ldc_I4_S ? ReadSByte() : ReadByte();
        return new ShortInlineIOperand
        {
            Value = value,
        };
    }
    
    private InlineBrTargetOperand ReadInlineBrTargetOperand()
    {
        var value = ReadInt32();
        return new InlineBrTargetOperand
        {
            Value = value,
        };
    }
    
    private Int32Operand ReadInt32Operand()
    {
        var value = ReadInt32();
        return new Int32Operand
        {
            Value = value,
        };
    }
    
    private SingleOperand ReadSingleOperand()
    {
        var value = ReadSingle();
        return new SingleOperand
        {
            Value = value,
        };
    }
    
    
    private DoubleOperand ReadDoubleOperand()
    {
        var value = ReadDouble();
        return new DoubleOperand
        {
            Value = value,
        };
    }
    
    private Int64Operand ReadInt64Operand()
    {
        var value = ReadInt64();
        return new Int64Operand
        {
            Value = value,
        };
    }
    
    private ByteOperand ReadByteOperand()
    {
        var value = ReadByte();
        return new ByteOperand
        {
            Value = value,
        };
    }
    
    private UInt16Operand ReadUInt16Operand()
    {
        var value = ReadUInt16();
        return new UInt16Operand
        {
            Value = value,
        };
    }
    
    private UInt32Operand ReadUInt32Operand()
    {
        var value = ReadUInt32();
        return new UInt32Operand
        {
            Value = value,
        };
    }
}