namespace OtelLineProfiler.IL;


public static class MethodSignatureFactory
{
    public static byte[] Create(byte callingConvention, byte returnType, int parameterCount, params byte[] parameters)
    {
        //TODO: ECMA page 59
        var stream = new MemoryStream();
        stream.WriteByte(callingConvention);
        stream.WriteByte((byte) parameterCount);
        stream.WriteByte(returnType);
        
        foreach (var parameter in parameters)
        {
            stream.WriteByte(parameter);
        }

        var signature = stream.ToArray();
        return signature;
    }
}