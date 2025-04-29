using Profiler.Core;

namespace LineProfiler.Instrumentation.Metadata;

public record InstrumentationMetadata(
    AssemblyId AssemblyId,
    FunctionId FunctionId,
    MdTypeRef BoolType,
    ActivityTypeRefs ActivityType,
    TraceIdTypeRefs TraceIdType,
    SignatureTokens SignatureTokens,
    uint LocalVarCount)
{
    public AssemblyId AssemblyId { get; set; } = AssemblyId;
    public FunctionId FunctionId { get; set; } = FunctionId;
    public MdTypeRef BoolType { get; set; } = BoolType;

    public ActivityTypeRefs ActivityType { get; set; } = ActivityType;
    public TraceIdTypeRefs TraceIdType { get; set; } = TraceIdType;
    public SignatureTokens SignatureTokens { get; set; } = SignatureTokens;
    public uint LocalVarCount { get; set; } = LocalVarCount;
}