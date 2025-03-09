namespace IlRewriter.Models;

[Flags]
public enum MethodHeaderFlags
{
    FatFormat = 0x3,
    TinyFormat = 0x2,
    MoreSects = 0x8,
    InitLocals = 0x10,
}

public class MethodHeader
{
    public int Size { get; set; }
    public int SizeInBytes { get; set; }
    public ushort MaxStack { get; set; }
    public int CodeSize { get; set; }
    public uint LocalVarSigToken { get; set; }
    public bool MoreSections { get; init; }
    public bool InitLocals { get; set; }
    public bool IsTiny { get; set; }
    public bool IsFat => !IsTiny;
}
