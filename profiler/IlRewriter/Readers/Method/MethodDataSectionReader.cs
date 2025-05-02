using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class MethodDataSectionReader(Stream stream, ReadContext context)
    : IlReader<MethodDataSection>(stream, context)
{
    public override MethodDataSection ReadFromStream()
    {
        var ehSections = Context.Header.MoreSections ? ReadSections() : [];
        return new MethodDataSection
        {
            EhSections = ehSections
        };
    }

    private List<MethodEhSection> ReadSections()
    {
        Align(4);
        
        var flags = ReadByte();
        var hasEhTable = (flags & 0x1) != 0;
        var hasOptIlTable = (flags & 0x2) != 0;
        var isFatFormat = (flags & 0x40) != 0;
        var hasMoreSections = (flags & 0x80) != 0;

        var ehSection = new MethodEhSection(isFatFormat, hasMoreSections, hasOptIlTable, hasEhTable);

        if (ehSection.IsFatFormat)
        {
            ReadFatSection(ehSection);
        }
        else
        {
            ReadSmallSection(ehSection);
        }

        var sections = new List<MethodEhSection> { ehSection };
        if (!ehSection.HasMoreSections) return sections;
        var nextSections = ReadSections();
        nextSections.ForEach(sections.Add);
        return sections;
    }

    private void ReadSmallSection(MethodEhSection ehSection)
    {
        var dataSize = ReadByte();
        var count = dataSize / 12;
        
        // Move over reserved space
        Advance(2);

        ReadExceptionHandlers(
            ehSection,
            count,
            () => ReadUInt16(),
            () => ReadByte());
    }

    private void ReadFatSection(MethodEhSection ehSection)
    {
        // Read data size from bytes 2-4
        Advance(-1);
        var dataSize = ReadInt32() >> 8;
        var count = dataSize / 24;

        ReadExceptionHandlers(
            ehSection,
            count,
            ReadInt32,
            ReadInt32);
    }

    private void ReadExceptionHandlers(MethodEhSection ehSection, int dataSize, Func<int> readEntry, Func<int> readLength)
    {
        for (var i = 0; i < dataSize; i++)
        {
            var handlerType = (ExceptionHandlerType)(readEntry() & 0x7);
            var tryStartOffset = readEntry();
            var tryEndOffset = tryStartOffset + readLength();

            var handlerStartOffset = readEntry();
            var handlerEndOffset = handlerStartOffset + readLength();

            var tryStart = GetInstructionAtEndOffset(tryStartOffset) ?? new Instruction { Offset = 0 };

            var tryEnd = GetInstructionAtEndOffset(tryEndOffset) ??
                         throw new NullReferenceException(nameof(tryEndOffset));

            var handlerStart = GetInstructionAtEndOffset(handlerStartOffset) ??
                               throw new NullReferenceException(nameof(handlerStartOffset));

            var handlerEnd = GetInstructionAtEndOffset(handlerEndOffset) ??
                             throw new NullReferenceException(nameof(handlerEndOffset));

            var exceptionHandler = new ExceptionHandler(handlerType, tryStart, tryEnd, handlerStart, handlerEnd);
            ReadExceptionHandlerSpecific(exceptionHandler);

            ehSection.Handlers.Add(exceptionHandler);
        }
    }

    private void ReadExceptionHandlerSpecific(ExceptionHandler handler)
    {
        switch (handler.HandlerType)
        {
            case ExceptionHandlerType.Catch:
                handler.CatchTypeToken = ReadUInt32();
                break;
            case ExceptionHandlerType.Filter:
                var filterOffSet = ReadInt32();
                handler.FilterStart = GetInstructionAtEndOffset(filterOffSet) ??
                                      throw new NullReferenceException(nameof(filterOffSet));
                break;
            case ExceptionHandlerType.Finally:
            case ExceptionHandlerType.Fault:
            default:
                Advance(4);
                break;
        }
    }
}