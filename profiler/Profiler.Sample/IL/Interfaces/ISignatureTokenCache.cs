using NativeObjects;
using Profiler.Core;

namespace Profiler.Sample.IL.Interfaces;

public interface ISignatureTokenCache
{
    public MdSignature GetMdSignature(AssemblyId assemblyId, byte[] signature, IMetadataEmitInvoker metadataEmit);
}