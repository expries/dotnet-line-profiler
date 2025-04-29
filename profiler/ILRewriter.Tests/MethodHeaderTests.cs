using IlRewriter.Readers.Method;
using IlRewriter.Writers;

namespace ILRewriter.Tests;

public class MethodHeaderTests : MethodTest
{
    [Theory]
    [MemberData(nameof(AllIlMethodBodies))]
    public void MethodHeader_CtorAndWriteToStream_ReturnsOriginalByteArray(params byte[] ilMethodBody)
    {
        var inputMemoryStream = new MemoryStream(ilMethodBody);
        var reader = new MethodHeaderReader(inputMemoryStream, new ReadContext());
        var header = reader.ReadFromStream();

        var methodHeaderBytes = ilMethodBody[..header.SizeInBytes];

        var outputMemoryStream = new MemoryStream();
        var writer = new MethodHeaderWriter();
        var binaryWriter = new BinaryStreamWriter(outputMemoryStream);

        writer.WriteToStream(header, binaryWriter);

        var serializedBytes = outputMemoryStream.ToArray();
        Assert.Equal(methodHeaderBytes, serializedBytes);
    }
}