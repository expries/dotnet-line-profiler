using Profiler.Core;

namespace LineProfiler.Instrumentation.Metadata;

public class TraceIdTypeRefs
{
    public MdTypeRef TraceId { get; set; }
    public MdMemberRef ToHexString { get; set; }
}