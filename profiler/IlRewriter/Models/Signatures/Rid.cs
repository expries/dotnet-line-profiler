namespace IlRewriter.Models.Signatures;

public enum RefType
{
    TypeDef,
    TypeRef,
    TypeSpec
}

public class Rid
{
    public RefType RefType;

    public Rid(uint value, RefType refType)
    {
        Value = value;
        RefType = refType;
    }

    public Rid(byte encodedTypeToken)
    {
        var tag = encodedTypeToken & 0x03;
        Value = (uint)encodedTypeToken >> 2;
        RefType = GetRefTypeForTag(tag);
    }

    public uint Value { get; }

    public static Rid FromTypeRef(int typeRef)
    {
        var value = (uint)((typeRef << 2) | 0x1);
        return new Rid(value, RefType.TypeRef);
    }

    private RefType GetRefTypeForTag(int tag)
    {
        return tag switch
        {
            0 => RefType.TypeDef,
            1 => RefType.TypeRef,
            2 => RefType.TypeSpec,
            _ => throw new Exception($"Unknown tag {tag}")
        };
    }

    public int ToTypeDefToken()
    {
        return 0x02000000 | (int)Value;
    }

    public int ToTypeRefToken()
    {
        return 0x01000000 | (int)Value;
    }

    public int ToTypeSpecToken()
    {
        return 0x1B000000 | (int)Value;
    }
}