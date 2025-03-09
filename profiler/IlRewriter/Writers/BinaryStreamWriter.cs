namespace IlRewriter.Writers;

public class BinaryStreamWriter : BinaryWriter
{
    public int Position {
        get { return (int) BaseStream.Position; }
        set { BaseStream.Position = value; }
    }

    public BinaryStreamWriter (Stream stream)
        : base (stream)
    {
    }

    public void WriteByte (byte value)
    {
        Write (value);
    }

    public void WriteUInt16 (ushort value)
    {
        Write (value);
    }

    public void WriteInt16 (short value)
    {
        Write (value);
    }

    public void WriteUInt32 (uint value)
    {
        Write (value);
    }

    public void WriteInt32 (int value)
    {
        Write (value);
    }

    public void WriteUInt64 (ulong value)
    {
        Write (value);
    }

    public void WriteBytes (byte [] bytes)
    {
        Write (bytes);
    }

    protected void Advance (int bytes)
    {
        BaseStream.Seek (bytes, SeekOrigin.Current);
    }

    public void Align (int align)
    {
        align--;
        var position = Position;
        var bytes = ((position + align) & ~align) - position;

        for (int i = 0; i < bytes; i++)
            WriteByte (0);
    }
}