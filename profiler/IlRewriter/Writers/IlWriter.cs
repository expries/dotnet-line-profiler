namespace IlRewriter.Writers;

public abstract class IlWriter<T>
{
    public abstract void WriteToStream(T item, Stream stream);
}