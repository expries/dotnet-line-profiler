namespace IlRewriter.Models.Signatures;

public class MethodSignature
{
    public byte[] BinaryData { get; set; }
    
    public MethodSignature(byte callingConvention, byte returnType, int parameterCount, params byte[] parameters)
    {
        var stream = new MemoryStream();
        stream.WriteByte(callingConvention);
        stream.WriteByte((byte) parameterCount);
        stream.WriteByte(returnType);
        
        foreach (var parameter in parameters)
        {
            stream.WriteByte(parameter);
        }

        BinaryData = stream.ToArray();
    }
}