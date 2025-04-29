using IlRewriter.Models.Signatures;
using Profiler.Core;

namespace LineProfiler.Instrumentation.Metadata;

public class ActivityTypeRefs(MemberRefSignature getTraceIdSignature)
{
    public MdTypeRef Activity { get; set; }
    public MdMemberRef Current { get; set; }
    public MdMemberRef GetTraceId { get; set; }
    public MemberRefSignature GetTraceIdSignature { get; set; } = getTraceIdSignature;
}