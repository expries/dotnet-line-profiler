using IlRewriter.Models;

namespace IlRewriter.Readers.Method;

public class MethodDataSectionReader(Stream stream, ReadContext context) 
    : IlReader<MethodDataSection>(stream, context)
{
    public override MethodDataSection ReadFromStream()
    {
        var ehSections = Context.Header.MoreSections ? ReadSection() : [];
        return new MethodDataSection
        {
            EhSections = ehSections
        };
    }

    private List<MethodEhSection> ReadSection()
    {
        var sections = new List<MethodEhSection>();
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
        
        sections.Add(ehSection);

        if (!ehSection.HasMoreSections) return sections;
        
        var moreEhSections = ReadSection();
        moreEhSections.ForEach(sections.Add);
        return sections;
    }
    
    private void ReadSmallSection(MethodEhSection ehSection)
    {
        var countByte = ReadByte();
        var count = countByte / 12;
        Advance(2);
        
        ReadExceptionHandlers (
            ehSection,
            count,
            () =>  ReadUInt16 (),
            () => ReadByte ());
    }
	
    private void ReadFatSection(MethodEhSection ehSection)
    {
        Advance(-1);
        var count = (ReadInt32 () >> 8) / 24;
        
        ReadExceptionHandlers (
            ehSection,
            count,
            ReadInt32,
            ReadInt32);
		
    }
	
    private void ReadExceptionHandlers (MethodEhSection ehSection, int count, Func<int> readEntry, Func<int> readLength)
    {
        for (var i = 0; i < count; i++)
        {
            var handlerType = (ExceptionHandlerType) (readEntry() & 0x7);
            var tryStartOffset = readEntry();
            var tryEndOffset = tryStartOffset + readLength();
            
            var handlerStartOffset = readEntry();
            var handlerEndOffset = handlerStartOffset + readLength();
            
            var tryStart = GetInstructionAtOffset(tryStartOffset);
            var tryEnd = GetInstructionAtOffset(tryEndOffset);
            var handlerStart = GetInstructionAtOffset(handlerStartOffset);
            var handlerEnd = GetInstructionAtOffset(handlerEndOffset);
            
            var exceptionHandler = new ExceptionHandler(handlerType, tryStart, tryEnd, handlerStart, handlerEnd);
            ReadExceptionHandlerSpecific(exceptionHandler);

            ehSection.Handlers.Add(exceptionHandler);
        }
    }
	
    private void ReadExceptionHandlerSpecific(ExceptionHandler handler)
    {
        switch (handler.HandlerType) {
            case ExceptionHandlerType.Catch:
                handler.CatchTypeToken = ReadUInt32();
                break;
            case ExceptionHandlerType.Filter:
                var filterOffSet = ReadInt32();
                handler.FilterStart = GetInstructionAtOffset(filterOffSet);
                break;
            case ExceptionHandlerType.Finally:
            case ExceptionHandlerType.Fault:
            default:
                Advance(4);
                break;
        }
    }
}