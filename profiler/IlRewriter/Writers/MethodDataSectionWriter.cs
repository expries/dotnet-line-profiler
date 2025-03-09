using IlRewriter.Models;
using IlRewriter.Readers;

namespace IlRewriter.Writers;

public class MethodDataSectionWriter : IlWriter<MethodDataSection>
{
    private BinaryStreamWriter _writer = new BinaryStreamWriter(new MemoryStream());
    
    public override void WriteToStream(MethodDataSection dataSection, Stream stream)
    {
        _writer = new BinaryStreamWriter(stream);
        
        foreach (var ehSection in dataSection.EhSections)
        {
            WriteEhSection(ehSection, stream);
        }
    }
    
    private void WriteEhSection(MethodEhSection ehSection, Stream stream)
    {
        Align(4, stream);
        
        if (ehSection.IsFatFormat || ehSection.Handlers.Count >= 0x15)
        {
            WriteFatSection(ehSection, stream);
        }
        else
        {
            WriteSmallSection(ehSection, stream);
        }
    }

    private void Align(int align, Stream stream)
    {
        align--;
        var position = stream.Position;
        var bytesToWrite = ((position + align) & ~align) - position;
        
        for (var i = 0; i < bytesToWrite; i++)
        {
            stream.WriteByte(0);
        }
    }

    private void WriteSmallSection(MethodEhSection ehSection, Stream stream)
    {

        
        WriteEhSectionFlags(ehSection, stream);
        var dataSize = ehSection.Handlers.Count * 12 + 4;
        
        var bytes = new[] { (byte) dataSize, (byte) 0, (byte) 0 };
        stream.Write(bytes);
        WriteExceptionHandlers(ehSection, i => _writer.WriteUInt16((ushort) i), i => _writer.WriteByte((byte) i));
    }

    private void WriteFatSection(MethodEhSection ehSection, Stream stream)
    {

        
        WriteEhSectionFlags(ehSection, stream);
        var dataSize = (ehSection.Handlers.Count * 24 + 4);
        
        _writer.WriteByte((byte) (dataSize & 0xff));
        _writer.WriteByte ((byte) ((dataSize >> 8) & 0xff));
        _writer.WriteByte ((byte) ((dataSize >> 16) & 0xff));
        
        WriteExceptionHandlers(ehSection, i => _writer.WriteInt32(i), i => _writer.WriteInt32(i));
    }
    
    private static void WriteEhSectionFlags(MethodEhSection ehSection, Stream stream)
    {
        byte flags = 0;
        if (ehSection.EhTable) flags |= 0x1;
        if (ehSection.OptIlTable) flags |= 0x2;
        if (ehSection.IsFatFormat) flags |= 0x40;
        if (ehSection.HasMoreSections) flags |= 0x80;
        
        var bytes = new[] { flags };
        stream.Write(bytes);
    }
    
    private void WriteExceptionHandlers(MethodEhSection ehSection, Action<int> writeEntry, Action<int> writeLength)
    {
        foreach (var exceptionHandler in ehSection.Handlers)
        {
            WriteExceptionHandler(exceptionHandler, writeEntry, writeLength);
        }
    }

    private void WriteExceptionHandler(ExceptionHandler handler, Action<int> writeEntry, Action<int> writeLength)
    {
        writeEntry((int) handler.HandlerType);
     
        writeEntry(handler.TryStart?.Offset ?? 0);
        writeLength(handler.TryEnd.Offset - (handler.TryStart?.Offset ?? 0));

        writeEntry(handler.HandlerStart?.Offset ?? 0);
        writeLength(handler.HandlerEnd.Offset - (handler.HandlerStart?.Offset ?? 0));
        
        WriteExceptionHandlerSpecific(handler);
    }
    

    
    private void WriteExceptionHandlerSpecific(ExceptionHandler handler)
    {
        if (handler.CatchTypeToken is not null)
        {
            
            _writer.WriteUInt32(handler.CatchTypeToken.Value);
        }
        else if (handler.FilterStart is not null)
        {
            _writer.WriteInt32(handler.FilterStart.Offset);
        }
        else
        {
            _writer.WriteInt32(0);
        }
    }
}