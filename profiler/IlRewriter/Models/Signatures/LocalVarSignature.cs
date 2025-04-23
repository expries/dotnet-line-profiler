namespace IlRewriter.Models.Signatures;

public static class SignatureCompressor
{
    public static uint DecompressUnsignedIntAt(byte[] bytes, int index)
    {
        var firstByte = bytes[index];
    
        if ((firstByte & 0x80) == 0) // 1-byte format (0xxxxxxx)
        {
            return firstByte;
        }
    
        if ((firstByte & 0x40) == 0) // 2-byte format (10xxxxxx xxxxxxxx)
        {
            var secondByte = bytes[index + 1];
            return ((uint) (firstByte & ~0x80) << 8) | secondByte;
        }
    
        if ((firstByte & 0xE0) == 0xC0) // 4-byte format (110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx)
        {
            if (index + 2 >= bytes.Length)
                throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
        
            var secondByte = bytes[index + 1];
            var thirdByte = bytes[index + 2];
            var fourthByte = bytes[index + 3];
        
            return ((uint) (firstByte & ~0xc0) << 24)
                   | (uint) secondByte << 16
                   | (uint) thirdByte << 8
                   | fourthByte;
        }

        throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
    }

    public static IEnumerable<byte> CompressUnsignedInt(uint value)
    {
        switch (value)
        {
            case <= 0x7F:
                // 0xxxxxxx
                yield return (byte)value;
                break;
            case <= 0x3FFF:
                // 10xxxxxx xxxxxxxx
                yield return (byte)((value >> 8) | 0x80);
                yield return (byte)(value & 0xFF);
                break;
            case <= 0x1FFF_FFFF:
                // 110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx
                yield return (byte)((value >> 24) | 0xC0);
                yield return (byte)((value >> 16) & 0xFF);
                yield return (byte)((value >> 8) & 0xFF);
                yield return (byte)(value & 0xFF);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), "Cannot compress > 0x1FFFFFFF");
        }
    }
}

public class LocalVarSignature
{
    public byte[] BinaryData { get; private set; }

    public LocalVarSignature(byte[] binaryData)
    {
        if (binaryData[0] != 0x07)
        {
            throw new ArgumentException("Local var signature is invalid");
        }

        BinaryData = binaryData;
    }   

    public void AddLocalVariable(byte[] localVariableSignature)
    {
        var newLength = BinaryData.Length + localVariableSignature.Length;
        var newData = new byte[newLength];
        var localsCount = GetVarCount();
        var newLocalsCount = localsCount + 1;
        
        newData[0] = 0x7;
        newData[1] = (byte) newLocalsCount;
        
        Array.Copy(BinaryData, 2, newData, 2, BinaryData.Length - 2);
        Array.Copy(localVariableSignature, 0, newData, BinaryData.Length, localVariableSignature.Length);
        
        BinaryData = newData;
    }

    public uint GetVarCount() => SignatureCompressor.DecompressUnsignedIntAt(BinaryData, 1);
}