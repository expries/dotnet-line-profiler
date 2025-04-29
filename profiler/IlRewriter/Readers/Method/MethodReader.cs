using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class MethodReader
{
    private readonly IlReader<MethodBody> _methodBodyReader;
    private readonly IlReader<MethodHeader> _methodHeaderReader;

    public MethodReader(Stream stream)
    {
        var readContext = new ReadContext();
        _methodHeaderReader = new MethodHeaderReader(stream, readContext);
        _methodBodyReader = new MethodBodyReader(stream, readContext);
    }

    public Models.Method ReadFromStream()
    {
        var header = _methodHeaderReader.ReadFromStream();
        var body = _methodBodyReader.ReadFromStream();
        return new Models.Method
        {
            Header = header,
            Body = body
        };
    }
}