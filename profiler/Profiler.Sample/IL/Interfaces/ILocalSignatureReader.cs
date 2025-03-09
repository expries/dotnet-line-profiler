using IlRewriter.Models;
using Profiler.Core;

namespace Profiler.Sample.IL.Interfaces;

public interface ILocalSignatureReader
{
    public LocalSignature ReadLocalSignature(MethodHeader header, MetaDataImport metaDataImport);
}