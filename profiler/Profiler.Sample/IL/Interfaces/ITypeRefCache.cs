using Profiler.Core;

namespace Profiler.Sample.IL.Interfaces;

public interface ITypeRefCache
{
    public MdTypeRef GetMdTypeRef(AssemblyId assemblyId, string typeName, MetaDataImport metaDataImport);
}