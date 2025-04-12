using NativeObjects;
using Profiler.Core;

namespace OtelLineProfiler.Caches;

public interface ISignatureTokenCache
{
    public MdSignature GetMdSignature(AssemblyId assemblyId, byte[] signature, IMetadataEmitInvoker metadataEmit);
}