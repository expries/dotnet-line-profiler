using IlRewriter.Models.Signatures;
using LineProfiler.Caches.Interfaces;
using LineProfiler.Helpers;
using Profiler.Core;

namespace LineProfiler.Caches;

public class TypeRefCache : ITypeRefCache
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
    


    private static unsafe MdTypeRef GetTypeRefForType(string typeName, MetaDataImport metaDataImport)
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
                if (!TryGetTypeRefName(typeRef, metaDataImport, out var refTypeName))
                {
                    continue;
                } 
                
                if (refTypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return typeRef;
                }
            }
        }

        throw new Exception($"Failed to get type ref for type '{typeName}'");
    }

    public static unsafe (MdMemberRef, byte[] signature) GetMemberRef(
        MdTypeRef parentTypeRef, 
        string memberName, 
        MetaDataImport metaDataImport)
    {
        const uint maxProperties = 4096;
        var pEnumerator = new HCORENUM();
        var memberArray = new MdMemberRef[maxProperties];
            
        fixed (MdMemberRef* pMember = memberArray)
        {
            var mdToken = new MdToken(parentTypeRef.Value);

            var hEnumMemberRefs = metaDataImport.EnumMemberRefs(
                &pEnumerator, mdToken, pMember, maxProperties, out var resultCount);

            if (!hEnumMemberRefs.IsOK)
            {
                throw new Exception(
                    $"Enum member refs for type {parentTypeRef.Value} failed with code {hEnumMemberRefs.Code} " +
                    $"({hEnumMemberRefs.ToString()})");
            }
            
            for (uint i = 0; i < resultCount; i++)
            {
                var memberRef = memberArray[i];

                if (!TryGetMemberRefProps(memberRef, metaDataImport, out var refMemberName, out var memberSignature))
                {
                    continue;
                }

                if (string.Equals(refMemberName, memberName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (memberRef, memberSignature);
                }
            }
        }
        
        throw new Exception($"Failed to get member ref for member '{memberName}' in type '{parentTypeRef.Value}'");
    }



    private static unsafe bool TryGetTypeRefName(MdTypeRef typeRef, MetaDataImport metaDataImport, out string typeName)
    {
        typeName = string.Empty;
        var typeNameBuffer = UnsafeTypes.CreateSpanForName();
        
        fixed (char* pTypeNameBuffer = typeNameBuffer)
        {
            var hGetRefProps = metaDataImport.GetTypeRefProps(
                typeRef, out _, pTypeNameBuffer, (uint) typeNameBuffer.Length, out var refTypeNameLength);
            
            if (!hGetRefProps.IsOK) return false;
            typeName = UnsafeTypes.GetStringFromCharPtr(pTypeNameBuffer, refTypeNameLength);
            return true;
        }
    }

    private static unsafe bool TryGetMemberRefProps(
        MdMemberRef memberRef, 
        MetaDataImport metaDataImport, 
        out string memberName, 
        out byte[] memberSignature)
    {
        memberName = string.Empty;
        memberSignature = [];
        var memberNameBuffer = UnsafeTypes.CreateSpanForName();
        
        fixed (char* pMemberNameBuffer = memberNameBuffer)
        {
            var hGetRef = metaDataImport.GetMemberRefProps(
                memberRef, 
                out _, 
                pMemberNameBuffer, 
                (uint) memberNameBuffer.Length, 
                out var memberNameLength, 
                out var signaturePtr, 
                out var signatureLength);
                    
            if (!hGetRef.IsOK) return false;
                    
            memberName = UnsafeTypes.GetStringFromCharPtr(pMemberNameBuffer, memberNameLength);
            memberSignature = UnsafeTypes.GetByteArrayFromPtr(signaturePtr, signatureLength);
            return true;
        }
    }
}