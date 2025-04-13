using LineProfiler.Helpers;
using Profiler.Core;

namespace LineProfiler.Caches;

public class InMemoryTypeRefCache : ITypeRefCache
{
    private readonly Dictionary<IntPtr, Dictionary<string, MdTypeRef>> _cache = new();
    
    public MdTypeRef GetMdTypeRef(AssemblyId assemblyId, string typeName, MetaDataImport metaDataImport)
    {
        if (!_cache.TryGetValue(assemblyId.Value, out var assemblyTypeRefs))
        {
            assemblyTypeRefs = new Dictionary<string, MdTypeRef>();
            _cache.Add(assemblyId.Value, assemblyTypeRefs);
        }
        
        if (!assemblyTypeRefs.TryGetValue(typeName, out var typeRef))
        {
            typeRef = GetTypeRefForType(typeName, metaDataImport);
            assemblyTypeRefs.Add(typeName, typeRef);
        }
        
        // e.g. System.Boolean
        return typeRef;
    }

    private unsafe MdTypeRef GetTypeRefForType(string typeName, MetaDataImport metaDataImport)
    {
        const uint maxTypeRefs = 4096;
        var pEnumerator = new HCORENUM();
        var typeRefArray = new MdTypeRef[maxTypeRefs];
        ulong resultCount = 0;
            
        fixed (MdTypeRef* pTypeRefArray = typeRefArray)
        {
            var hEnumTypeRefs = metaDataImport.EnumTypeRefs(&pEnumerator, pTypeRefArray, maxTypeRefs, &resultCount);

            if (!hEnumTypeRefs.IsOK)
            {
                throw new Exception("Enum type refs failed");
            }
        
            for (uint i = 0; i < resultCount; i++)
            {
                var typeRef = pTypeRefArray[i];
                var typeNameBuffer = UnsafeTypes.CreateSpanForName();
                fixed (char* pTypeNameBuffer = typeNameBuffer)
                {
                    var hGetRef = metaDataImport.GetTypeRefProps(typeRef, out _, pTypeNameBuffer, (uint) typeNameBuffer.Length, out var refTypeNameLength);
                    if (!hGetRef.IsOK) continue;
                
                    var refTypeName = UnsafeTypes.GetStringFromCharPtr(pTypeNameBuffer, refTypeNameLength);
                    if (refTypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) return typeRef;
                }
            }
        }

        throw new Exception($"Failed to get type ref for type '{typeName}'");
    }
}