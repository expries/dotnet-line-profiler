using IlRewriter.Models;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public interface ILocalVarSignatureReader
{
    public LocalVarSignature ReadLocalVarSignature(MethodHeader header, MetaDataImport metaDataImport);
}