using IlRewriter.Models.Signatures;
using Profiler.Core;

namespace LineProfiler.Instrumentation;

public class ActivityTypeRefs(MemberRefSignature getTraceIdSignature)
{
    public MdTypeRef Activity { get; set; }
    public MdMemberRef Current { get; set; }
    public MdMemberRef GetTraceId { get; set; }
    public MemberRefSignature GetTraceIdSignature { get; set; } = getTraceIdSignature;
}

public class TraceIdTypeRefs
{
    public MdTypeRef TraceId { get; set; }
    public MdMemberRef ToHexString { get; set; }
}

public class SignatureTokens
{
    public MdSignature Callback { get; set; }
    public MdSignature LocalVars { get; set; }
}

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