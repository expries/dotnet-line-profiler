namespace IlRewriter.Models.Signatures;

public class MemberRefSignature
{
    public enum RefType
    {
        TypeDef = 0,
        TypeRef = 1,
        TypeSpec = 2
    }
    
    public static bool TryParseComplexReturnType(byte[] binarySignature, out RefType refType, out int rid)
    {
        refType = default;
        rid = -1;

        if (binarySignature[2] != 0x11)
        {
            return false;
        }
        
        var encoded = binarySignature[3];
        var tag = encoded & 0x03;
        rid = encoded >> 2;

        switch (tag)
        {
            case 0: 
                refType = RefType.TypeDef; 
                return true;
            case 1: 
                refType = RefType.TypeRef; 
                return true;
            case 2: 
                refType = RefType.TypeSpec; 
                return true;
        }

        return false;
    }
}