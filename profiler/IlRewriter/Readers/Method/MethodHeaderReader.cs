using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class MethodHeaderReader : IlReader<MethodHeader>
{
    public MethodHeaderReader(Stream stream, ReadContext context) : base(stream, context)
    {
    }

    public override MethodHeader ReadFromStream()
    {
        // 2 least significant bit of the first byte indicate what kind of header is present
        var firstByte = ReadByte();
        var header = (firstByte & 0x3) switch
        {
            0x2 => ReadTinyHeader(firstByte),
            0x3 => ReadFatHeader(),
            _ => throw new InvalidOperationException()
        };

        Context.Header = header;
        return header;
    }

    private MethodHeader ReadTinyHeader(byte firstByte)
    {
        var bodySizeInBytes = firstByte >> 2;

        return new MethodHeader
        {
            Size = 1,
            MaxStack = 8,
            CodeSize = bodySizeInBytes,
            LocalVarSigToken = 0,
            SizeInBytes = 1,
            IsTiny = true,
            MoreSections = false,
            InitLocals = false
        };
    }

    private MethodHeader ReadFatHeader()
    {
        Advance(-1);

        var firstWord = ReadUInt16();

        var flags = firstWord & 0b0000111111111111;
        var size = firstWord >> 12;

        var maxStack = ReadUInt16();
        var codeSize = ReadInt32();
        var localVarSigToken = ReadUInt32();

        var moreSections = (flags & 0x8) != 0;
        var initLocals = (flags & 0x10) != 0;

        return new MethodHeader
        {
            Size = size,
            MaxStack = maxStack,
            CodeSize = codeSize,
            LocalVarSigToken = localVarSigToken,
            SizeInBytes = size * 4, // *4 because size is DWORD
            IsTiny = false,
            MoreSections = moreSections,
            InitLocals = initLocals
        };
    }
}