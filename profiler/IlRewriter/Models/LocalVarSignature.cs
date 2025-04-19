namespace IlRewriter.Models;

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

    public uint GetVarCount()
    {
        return DecompressUnsignedIntAt(1);
    }

    private uint DecompressUnsignedIntAt(int index)
    {
        var firstByte = BinaryData[index];
    
        if ((firstByte & 0x80) == 0) // 1-byte format (0xxxxxxx)
        {
            return firstByte;
        }
    
        if ((firstByte & 0x40) == 0) // 2-byte format (10xxxxxx xxxxxxxx)
        {
            var secondByte = BinaryData[index + 1];
            return ((uint) (firstByte & ~0x80) << 8) | secondByte;
        }
    
        if ((firstByte & 0xE0) == 0xC0) // 4-byte format (110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx)
        {
            if (index + 2 >= BinaryData.Length)
                throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
        
            var secondByte = BinaryData[index + 1];
            var thirdByte = BinaryData[index + 2];
            var fourthByte = BinaryData[index + 3];
        
            return ((uint) (firstByte & ~0xc0) << 24)
                   | (uint) secondByte << 16
                   | (uint) thirdByte << 8
                   | fourthByte;
        }

        throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
    } 
}