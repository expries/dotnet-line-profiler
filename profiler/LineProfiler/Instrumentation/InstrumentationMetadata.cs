using Profiler.Core;

namespace LineProfiler.Instrumentation;

public record InstrumentationMetadata
{
    // TODO: clean up
    public AssemblyId AssemblyId { get; set; }
    public FunctionId FunctionId { get; set; }
    public MdTypeRef BoolType { get; set; }
    public MdTypeRef TraceIdRef { get; set; }
    public MdMemberRef GetCurrentActivity { get; set; }
    public MdMemberRef GetTraceId { get; set; }
    public MdMemberRef TraceIdToHexString { get; set; }
    public MdSignature CallbackSignature { get; set; }
    public MdSignature LocalVarSignature { get; set; }
    public uint LocalVarCount { get; set; }
}