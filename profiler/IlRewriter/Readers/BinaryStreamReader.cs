namespace IlRewriter.Readers;

public class BinaryStreamReader(Stream stream) : BinaryReader(stream)
{
    public int Position {
        get => (int) BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public int Length => (int) BaseStream.Length;

    public void Advance(int bytes)
    {
        BaseStream.Seek(bytes, SeekOrigin.Current);
    }

    public ushort Read2Bytes()
    {
        return ReadUInt16();
    }

    public int Read3Bytes()
    {
        Advance(-1);
        var fourBytes = ReadInt32();
        var threeBytes = fourBytes >> 8;
        return threeBytes;
    }

    public int Read4Bytes()
    {
        return ReadInt32();
    }

    public void MoveTo(uint position)
    {
        BaseStream.Seek(position, SeekOrigin.Begin);
    }

    public void Align(int align)
    {
        align--;
        var position = Position;
        Advance (((position + align) & ~align) - position);
    }
}