using IlRewriter.Models;
using Profiler.Core;

namespace OtelLineProfiler.IL;

public interface ILocalVarSignatureReader
{
    public LocalSignature ReadLocalVarSignature(MethodHeader header, MetaDataImport metaDataImport);
}