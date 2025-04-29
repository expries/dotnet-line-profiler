using Profiler.Core;

namespace LineProfiler.Instrumentation.Metadata;

public class SignatureTokens
{
    public MdSignature Callback { get; set; }
    public MdSignature LocalVars { get; set; }
}