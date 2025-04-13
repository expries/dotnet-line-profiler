using System.Text.Json;
using IlRewriter.Models;

namespace IlRewriter.Writers;

public class MethodWriter : IlWriter<Method>
{
    private readonly MethodHeaderWriter _headerWriter = new MethodHeaderWriter();
    private readonly MethodBodyWriter _bodyWriter = new MethodBodyWriter();
    
    public override void WriteToStream(Method instruction, Stream stream)
    {
        var methodBodyStream = new MemoryStream();
        var codeSize = _bodyWriter.WriteToStream(instruction.Body, methodBodyStream);

        PrepareHeader(instruction, codeSize);
        
        var headerWriter = new BinaryStreamWriter(stream);
        _headerWriter.WriteToStream(instruction.Header, headerWriter);

        var bodyStreamArray = methodBodyStream.ToArray();
        stream.Write(bodyStreamArray, 0, bodyStreamArray.Length);
    }

    private static void PrepareHeader(Method method, long codeSize)
    {
        method.Header.CodeSize = (int) codeSize;

        if (codeSize >= 64 || 
            method.Header.LocalVarSigToken > 0 ||
            method.Header.MaxStack > 8 ||
            method.Header.InitLocals ||
            method.Header.MoreSections ||
            method.Body.DataSection.EhSections.Count > 0)
        {
            method.Header.IsTiny = false;
            method.Header.SizeInBytes = 12;
            method.Header.Size = 3;
        }
        else
        {
            method.Header.IsTiny = true;
            method.Header.Size = 1;
            method.Header.SizeInBytes = 4;
        }
    }
}