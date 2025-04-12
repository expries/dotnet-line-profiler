namespace OtelLineProfiler.IL;

public class LocalVarSignature
{
    public byte[] Data { get; set; }

    public LocalVarSignature(byte[] data)
    {
        Data = data;
    }

    public void AddLocalVariable(byte[] localVariableSignature)
    {
        var newLength = Data.Length + localVariableSignature.Length;
        var newData = new byte[newLength];
        var localsCount = GetVarCount();
        var newLocalsCount = localsCount + 1;
        
        newData[0] = 0x7;
        newData[1] = (byte) newLocalsCount;
        
        Array.Copy(Data, 2, newData, 2, Data.Length - 2);
        Array.Copy(localVariableSignature, 0, newData, Data.Length, localVariableSignature.Length);
        
        Data = newData;
    }

    public uint GetVarCount()
    {
        return DecompressUnsignedIntAt(1);
    }

    private uint DecompressUnsignedIntAt(int index)
    {
        var firstByte = Data[index++];
    
        if ((firstByte & 0x80) == 0) // 1-byte format (0xxxxxxx)
        {
            return firstByte;
        }
    
        if ((firstByte & 0x40) == 0) // 2-byte format (10xxxxxx xxxxxxxx)
        {
            var secondByte = Data[index++];
            return ((uint) (firstByte & ~0x80) << 8) | secondByte;
        }
    
    
        if ((firstByte & 0xE0) == 0xC0) // 4-byte format (110xxxxx xxxxxxxx xxxxxxxx xxxxxxxx)
        {
            if (index + 2 >= Data.Length)
                throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
        
            var secondByte = Data[index++];
            var thirdByte = Data[index++];
            var fourthByte = Data[index++];
        
            return ((uint) (firstByte & ~0xc0) << 24)
                   | (uint) secondByte << 16
                   | (uint) thirdByte << 8
                   | fourthByte;
        }

        throw new ArgumentException("Unexpected end of data while decoding compressed integer.");
    } 
}