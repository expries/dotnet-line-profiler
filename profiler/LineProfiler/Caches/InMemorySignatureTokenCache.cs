using NativeObjects;
using OtelLineProfiler.Helpers;
using Profiler.Core;

namespace OtelLineProfiler.Caches;

public class InMemorySignatureTokenCache : ISignatureTokenCache
{
    private readonly Dictionary<IntPtr, Dictionary<byte[], MdSignature>> _cache = new();
    
    public unsafe MdSignature GetMdSignature(AssemblyId assemblyId, byte[] signature, IMetadataEmitInvoker metadataEmit)
    {
        if (!_cache.TryGetValue(assemblyId.Value, out var assemblySignatures))
        {
            assemblySignatures = new Dictionary<byte[], MdSignature>();
            _cache.Add(assemblyId.Value, assemblySignatures);
        }

        if (assemblySignatures.TryGetValue(signature, out var mdSignature))
        {
            return mdSignature;
        }
        
        var signaturePtr = UnsafeTypes.GetBytePtr(signature);
        var hGetToken = metadataEmit.GetTokenFromSig(signaturePtr, (uint) signature.Length, out var signatureToken);

        if (hGetToken is { IsOK: false, IsDuplicate: false })
        {
            throw new Exception($"Failed to get token for signature '{BitConverter.ToString(signature)}': {hGetToken.Code}");
        }
        
        _cache[assemblyId.Value].Add(signature, signatureToken);
        return signatureToken;
    }
}