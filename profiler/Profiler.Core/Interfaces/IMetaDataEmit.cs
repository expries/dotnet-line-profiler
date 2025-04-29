using System.Runtime.InteropServices;

namespace Profiler.Core.Interfaces;

[NativeObject]
public unsafe interface IMetadataEmit : IUnknown
{
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    void SetModuleProps([MarshalAs(UnmanagedType.LPWStr)] MdString szName);

    void Save([MarshalAs(UnmanagedType.LPWStr)] MdString szFile, uint dwSaveFlags);


    void SaveToStream([MarshalAs(UnmanagedType.Interface)] IStream* pIStream, uint dwSaveFlags);

    uint GetSaveSize(uint fSave);

    uint DefineTypeDef([MarshalAs(UnmanagedType.LPWStr)] MdString szTypeDef,
        uint dwTypeDefFlags,
        uint tkExtends,
        uint* rtkImplements);

    uint DefineNestedType([MarshalAs(UnmanagedType.LPWStr)] MdString szTypeDef,
        uint dwTypeDefFlags,
        uint tkExtends,
        uint* rtkImplements,
        uint tdEncloser);

    void SetHandler([MarshalAs(UnmanagedType.IUnknown)] IUnknown* pUnk);

    uint DefineMethod(uint td,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szName,
        uint dwMethodFlags,
        byte* pvSigBlob,
        uint cbSigBlob,
        uint ulCodeRva,
        uint dwImplFlags);

    void DefineMethodImpl(uint td, uint tkBody, uint tkDecl);

    uint DefineTypeRefByName(uint tkResolutionScope,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szName);

    uint DefineImportType([MarshalAs(UnmanagedType.Interface)] IMetaDataAssemblyImport* pAssemImport,
        IntPtr pbHashValue,
        uint cbHashValue,
        uint tkImport,
        [MarshalAs(UnmanagedType.Interface)] IMetaDataImport* pImport,
        uint tdImport);

    uint DefineMemberRef(uint tkImport,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szName,
        byte* pvSigBlob,
        uint cbSigBlob);

    uint DefineImportMember([MarshalAs(UnmanagedType.Interface)] IMetaDataAssemblyImport* pAssemImport,
        IntPtr pbHashValue,
        uint cbHashValue,
        [MarshalAs(UnmanagedType.Interface)] IMetaDataImport* pImport,
        uint mbMember,
        [MarshalAs(UnmanagedType.Interface)] IMetaDataImport* pImportTd,
        uint tkImport,
        byte* pvSigBlob,
        uint cbSigBlob);

    uint DefineEvent(uint td,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szEvent,
        uint dwEventFlags,
        uint tkEventType,
        uint mdAddOn,
        uint mdRemoveOn,
        uint mdFire,
        uint* rmdOtherMethods);

    void SetClassLayout(uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize);

    void DeleteClassLayout(uint td);

    void SetFieldMarshal(uint tk, IntPtr pvNativeType, uint cbNativeType);

    void DeleteFieldMarshal(uint tk);

    uint DefinePermissionSet(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission);

    void SetRva(uint md, uint ulRva);

    HResult GetTokenFromSig(byte* pvSig, uint cbSig, out MdSignature signature);

    void DefineModuleRef([MarshalAs(UnmanagedType.LPWStr)] MdString szName, out uint pmur);

    void SetParent(uint mr, uint tk);

    uint GetTokenFromTypeSpec(byte* pvSig, uint cbSig);

    void SaveToMemory(IntPtr pbData, uint cbData);

    uint DefineUserString([MarshalAs(UnmanagedType.LPWStr)] MdString szString, uint cchString);

    void DeleteToken(uint tkObj);

    void SetMethodProps(uint md,
        uint dwMethodFlags,
        uint ulCodeRva,
        uint dwImplFlags);

    void SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, uint* rtkImplements);

    void SetEventProps(uint ev,
        uint dwEventFlags,
        uint tkEventType,
        uint mdAddOn,
        uint mdRemoveOn,
        uint mdFire,
        uint* rmdOtherMethods);

    uint SetPermissionSetProps(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission);

    void DefinePinvokeMap(uint tk,
        uint dwMappingFlags,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szImportName,
        uint mrImportDll);

    void SetPinvokeMap(uint tk, uint dwMappingFlags, [MarshalAs(UnmanagedType.LPWStr)] MdString szImportName,
        uint mrImportDll);

    void DeletePinvokeMap(uint tk);

    uint DefineCustomAttribute(uint tkObj, uint tkType, IntPtr pCustomAttribute, uint cbCustomAttribute);

    void SetCustomAttributeValue(uint tkObj, uint tkType, IntPtr pCustomAttribute, uint cbCustomAttribute);

    uint DefineField(uint td,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szName,
        uint dwFieldFlags,
        byte* pvSigBlob,
        uint cbSigBlob,
        uint dwCPlusTypeFlag,
        IntPtr pValue,
        uint cchValue);

    void DefineProperty(uint td,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szProperty,
        uint dwPropFlags,
        byte* pvSigBlob,
        uint cbSigBlob,
        uint dwCPlusTypeFlag,
        IntPtr pValue,
        uint cchValue,
        uint mdSetter,
        uint mdGetter,
        uint* rmdOtherMethods);

    uint DefineParam(uint md,
        uint ulParamSeq,
        [MarshalAs(UnmanagedType.LPWStr)] MdString szName,
        uint dwParamFlags,
        uint dwCPlusTypeFlag,
        IntPtr pValue,
        uint cchValue);

    void SetFieldProps(uint fd,
        uint dwFieldFlags,
        uint dwCPlusTypeFlag,
        IntPtr pValue,
        uint cchValue);

    void SetPropertyProps(uint pr,
        uint dwPropFlags,
        uint dwCPlusTypeFlag,
        IntPtr pValue,
        uint cchValue,
        uint mdSetter,
        uint mdGetter,
        uint* rmdOtherMethods);

    void SetParamProps(uint pd, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue);

    void DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs, out uint pulErrorAttr);

    void ApplyEditAndContinue([MarshalAs(UnmanagedType.Interface)] object* pImport);

    uint TranslateSigWithScope([MarshalAs(UnmanagedType.Interface)] IMetaDataAssemblyImport* pAssemImport,
        IntPtr pbHashValue,
        uint cbHashValue,
        [MarshalAs(UnmanagedType.Interface)] IMetaDataImport* pImport,
        byte* pvSigBlob,
        uint cbSigBlob,
        [MarshalAs(UnmanagedType.Interface)] IMetaDataAssemblyEmit* pAssemEmit,
        [MarshalAs(UnmanagedType.Interface)] IMetadataEmit* pEmit,
        byte* pvTranslatedSig,
        uint cbTranslatedSig);

    void SetMethodImplFlags(uint md, uint dwImplFlags);

    void SetFieldRva(uint fd, uint ulRva);

    void Merge([MarshalAs(UnmanagedType.Interface)] IMetaDataImport* pImport,
        [MarshalAs(UnmanagedType.Interface)] IMetadataEmit* pEmit,
        uint* rTokens);

    void MergeEnd();
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}

public interface IMetaDataAssemblyImport
{
}

public interface IMetaDataAssemblyEmit
{
}

[ComImport]
[Guid("0000000C-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IStream
{
}