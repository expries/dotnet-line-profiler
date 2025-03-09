namespace IlRewriter.Models;

public static class EhFlags
{
    public const int EhTable = 0x1;
    public const int OptIlTable = 0x2;
    public const int FatFormat = 0x40;
    public const int MoreSects = 0x80;
}

public static class HeaderFlags
{
    public const int FatFormat = 0x3;
    public const int TinyFormat = 0x2;
    public const int MoreSects = 0x8;
    public const int InitLocals = 0x10;
}

public enum ExceptionHandlerType {
    Catch = 0,
    Filter = 1,
    Finally = 2,
    Fault = 4,
}

public class MethodEhSection(bool isFatFormat, bool hasMoreSections, bool optIlTable, bool ehTable)
{
    public bool IsFatFormat { get; set; } = isFatFormat;
    public bool HasMoreSections { get; set; } = hasMoreSections;
    public bool OptIlTable { get; set; } = optIlTable;
    public bool EhTable { get; set; } = ehTable;
    
    public List<ExceptionHandler> Handlers { get; set; } = [];
}