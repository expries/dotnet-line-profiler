using IlRewriter.Models;
using IlRewriter.Readers.Method;

namespace IlRewriter.Readers;

public abstract class IlReader<T> : BinaryStreamReader
{
    public abstract T ReadFromStream();

    protected int Offset => Position - Context.Header.SizeInBytes;
    protected int EndPosition => Context.Header.SizeInBytes + Context.Header.CodeSize;
    protected readonly ReadContext Context;
    
    protected IlReader(Stream stream, ReadContext context) : base(stream)
    {
        Context = context;
    }

    protected Instruction? GetInstructionAtEndOffset(int offset)
    {
        Context.InstructionsByEndOffset.TryGetValue(offset, out var instruction);
        return instruction;
    }
}