using IlRewriter.Models;
using OtelLineProfiler.Helpers;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public class LocalVarVarSignatureReader : ILocalVarSignatureReader
{
    private static readonly LocalSignature EmptyLocalVarSignature = new([0x7, 0x0]);

    public unsafe LocalSignature ReadLocalVarSignature(MethodHeader header, MetaDataImport metaDataImport)
    {
        if (header.LocalVarSigToken <= 0) return EmptyLocalVarSignature;
        
        var token = new MdSignature { Value = (int) header.LocalVarSigToken };
        var hResult = metaDataImport.GetSigFromToken(token, out var ppvSig, out var pcbSig);

        if (!hResult.IsOK)
        {
            throw new ArgumentException($"Failed to obtain local var signature from token '{token.Value}'");
        }
        
        var signature = TypeHelper.GetByteArrayFromPointer(ppvSig, pcbSig);

        if (signature[0] != 0x07)
        {
            throw new ArgumentException("Local var signature is invalid");
        }

        return new LocalSignature(signature); 
    }
}