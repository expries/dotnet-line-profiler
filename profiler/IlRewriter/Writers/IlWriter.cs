namespace IlRewriter.Writers;

public abstract class IlWriter<T>
{
    public abstract void WriteToStream(T instruction, Stream stream);
}