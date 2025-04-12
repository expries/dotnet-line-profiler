using IlRewriter.Models;
using LineProfiler.Helpers;
using Profiler.Core;

namespace LineProfiler.IL;

public class LocalVarSignatureReader : ILocalVarSignatureReader
{
    private static readonly LocalVarSignature EmptyLocalVarVarSignature = new([0x7, 0x0]);

    public unsafe LocalVarSignature ReadLocalVarSignature(MethodHeader header, MetaDataImport metaDataImport)
    {
        if (header.LocalVarSigToken <= 0) return EmptyLocalVarVarSignature;
        
        var token = new MdSignature { Value = (int) header.LocalVarSigToken };
        var hResult = metaDataImport.GetSigFromToken(token, out var ppvSig, out var pcbSig);

        if (!hResult.IsOK)
        {
            throw new ArgumentException($"Failed to obtain local var signature from token '{token.Value}'");
        }
        
        var signature = UnsafeTypes.GetByteArrayFromPtr(ppvSig, pcbSig);
        return new LocalVarSignature(signature); 
    }
}