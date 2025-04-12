using IlRewriter.Models;
using Profiler.Core;

namespace LineProfiler.IL;

public interface ILocalVarSignatureReader
{
    public LocalVarSignature ReadLocalVarSignature(MethodHeader header, MetaDataImport metaDataImport);
}