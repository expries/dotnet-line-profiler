namespace Profiler.Core.Interfaces;

[NativeObject]
public unsafe interface IMetaDataAssemblyEmit : IUnknown
{
    HResult DefineAssembly(
        byte* publicKey,
        uint publicKeySize,
        uint hashAlgId,
        char* szName,
        ASSEMBLYMETADATA* metadata,
        uint assemblyFlags,
        out MdAssemblyRef assemblyToken
    );

    HResult DefineAssemblyRef(
        byte* publicKeyOrToken,
        uint publicKeyOrTokenSize,
        char* szName,
        ASSEMBLYMETADATA* metadata,
        byte* hashValue,
        uint hashValueSize,
        uint assemblyRefFlags,
        out MdAssemblyRef assemblyRefToken
    );

    HResult DefineFile(
        char* szName,
        byte* hashValue,
        uint hashValueSize,
        uint fileFlags,
        out MdToken fileToken
    );

    HResult DefineExportedType(
        char* szName,
        MdToken implementationToken,
        MdTypeDef typeDefToken,
        uint exportedTypeFlags,
        out MdToken exportedTypeToken
    );


    HResult DefineManifestResource(
        char* szName,
        MdToken implementationToken,
        uint offset,
        uint resourceFlags,
        out MdToken manifestResourceToken
    );

    HResult SetAssemblyProps(
        uint assemblyToken,
        byte* publicKey,
        uint publicKeySize,
        uint hashAlgId,
        char* szName,
        ASSEMBLYMETADATA* metadata,
        uint assemblyFlags
    );

    HResult SetAssemblyRefProps(
        uint assemblyRefToken,
        byte* publicKeyOrToken,
        uint publicKeyOrTokenSize,
        char* szName,
        ASSEMBLYMETADATA* metadata,
        byte* hashValue,
        uint hashValueSize,
        uint assemblyRefFlags
    );

    HResult SetFileProps(
        MdToken fileToken,
        byte* hashValue,
        uint hashValueSize,
        uint fileFlags
    );

    HResult SetExportedTypeProps(
        /* [in] */ MdToken tkExportedType,
        /* [in] */ char* szName,
        /* [in] */ MdToken tkImplementation,
        /* [in] */ MdTypeDef tkTypeDef,
        /* [in] */ uint dwExportedTypeFlags
    );

    HResult SetManifestResourceProps(
        /* [in] */ MdToken tkManifestResource,
        /* [in] */ char* szName,
        /* [in] */ MdToken tkImplementation,
        /* [in] */ uint dwOffset,
        /* [in] */ uint dwResourceFlags
    );
}