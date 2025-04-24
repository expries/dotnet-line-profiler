using NativeObjects;
using Profiler.Core;

namespace LineProfiler.Caches.Interfaces;

public interface ISignatureTokenCache
{
    public MdSignature GetMdSignature(AssemblyId assemblyId, byte[] signature, IMetadataEmitInvoker metadataEmit);
}