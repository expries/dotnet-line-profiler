using Profiler.Core;

namespace LineProfiler.Caches;

public interface ITypeRefCache
{
    public MdTypeRef GetMdTypeRef(AssemblyId assemblyId, string typeName, MetaDataImport metaDataImport);
}