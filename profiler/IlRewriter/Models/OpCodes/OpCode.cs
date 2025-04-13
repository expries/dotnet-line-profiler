using IlRewriter.Models.OpCodes.Enums;

namespace IlRewriter.Models.OpCodes;

public readonly struct OpCode 
{
    public byte Op1 { get; }
    public byte Op2 { get; }
    
    public string Name => OpCodeNames.Names [_code];
    public OperandType OperandType => (OperandType) _operandType;
    public int SizeInBytes => Op1 == 0xff ? 1 : 2;

    private readonly byte _code;
    private readonly byte _operandType;
    
    internal OpCode (int x, int y)
    {
        Op1 = (byte) ((x >> 0) & 0xff);
        Op2 = (byte) ((x >> 8) & 0xff);
        _code = (byte) ((x >> 16) & 0xff);
        _operandType = (byte) ((y >> 8) & 0xff);

        if (Op1 == 0xff)
        {
            OpCodes.OneByteOpCode [Op2] = this;
        }
        else
        {
            OpCodes.TwoBytesOpCode [Op2] = this;
        }
    }

    public static bool operator ==(OpCode one, OpCode other)
    {
        return one.Op1 == other.Op1 && one.Op2 == other.Op2;
    }

    public static bool operator !=(OpCode one, OpCode other)
    {
        return one.Op1 != other.Op1 || one.Op2 != other.Op2;
    }

    public override bool Equals(object? obj)
    {
        return obj is OpCode other && this == other;
    }

    public bool Equals(OpCode other)
    {
        return _code == other._code && _operandType == other._operandType && Op1 == other.Op1 && Op2 == other.Op2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_code, _operandType, Op1, Op2);
    }
}