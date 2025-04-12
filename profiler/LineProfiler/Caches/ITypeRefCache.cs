using Profiler.Core;

namespace OtelLineProfiler.Caches;

public interface ITypeRefCache
{
    public MdTypeRef GetMdTypeRef(AssemblyId assemblyId, string typeName, MetaDataImport metaDataImport);
}