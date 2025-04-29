namespace IlRewriter.Models.Signatures;

public class MemberRefSignature(byte[] binaryData)
{
    public bool TryParseReturnType(out Rid rid)
    {
        rid = new Rid(0, RefType.TypeSpec);

        // Note: only supports complex types
        if (binaryData[2] != 0x11) return false;

        var encodedTypeToken = binaryData[3];

        try
        {
            rid = new Rid(encodedTypeToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}