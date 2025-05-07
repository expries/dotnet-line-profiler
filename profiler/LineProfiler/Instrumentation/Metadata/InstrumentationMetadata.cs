using Profiler.Core;

namespace LineProfiler.Instrumentation.Metadata;

public record InstrumentationMetadata(
    MdToken FunctionToken,
    MdTypeRef BoolType,
    MdMemberRef CallbackRef,
    MdSignature LocalVarSignature,
    uint LocalVarCount)
{
    public MdToken FunctionToken { get; set; } = FunctionToken;
    public MdTypeRef BoolType { get; set; } = BoolType;
    public MdMemberRef CallbackRef { get; set; } = CallbackRef;
    public MdSignature LocalVarSignature { get; set; } = LocalVarSignature;
    public uint LocalVarCount { get; set; } = LocalVarCount;
}