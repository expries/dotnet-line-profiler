using IlRewriter.Models;

namespace IlRewriter.Writers;

public class MethodHeaderWriter
{
    public void WriteToStream(MethodHeader header, BinaryStreamWriter writer)
    {
        if (header.IsTiny)
            WriteTinyHeader(header, writer);
        else
            WriteFatHeader(header, writer);
    }

    private void WriteTinyHeader(MethodHeader header, BinaryStreamWriter writer)
    {
        var headerByte = (byte)((header.CodeSize << 2) | 0x2);
        writer.WriteByte(headerByte);
    }

    private void WriteFatHeader(MethodHeader header, BinaryStreamWriter writer)
    {
        ushort flags = 0;

        flags |= 0x3;
        if (header.MoreSections) flags |= 0x8;
        if (header.InitLocals) flags |= 0x10;

        var firstWord = (ushort)((header.Size << 12) | flags);

        writer.WriteUInt16(firstWord);

        writer.WriteUInt16(header.MaxStack);
        writer.WriteInt32(header.CodeSize);
        writer.WriteUInt32(header.LocalVarSigToken);
    }
}