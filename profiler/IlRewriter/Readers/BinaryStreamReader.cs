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