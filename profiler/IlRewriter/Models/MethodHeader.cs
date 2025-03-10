namespace IlRewriter.Models;

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
