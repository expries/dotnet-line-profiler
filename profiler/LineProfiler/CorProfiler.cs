using IlRewriter.Models;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using LineProfiler.Caches;
using LineProfiler.Configuration;
using LineProfiler.DebugInfo;
using LineProfiler.Helpers;
using LineProfiler.Instrumentation;
using LineProfiler.Instrumentation.Metadata;
using LineProfiler.Processing;
using NativeObjects;
using Profiler.Core;

namespace LineProfiler;

internal unsafe class CorProfiler : CorProfilerCallback10Base
{
    private TypeRefCache TypeRefCache { get; } = new();
    private SignatureTokenCache SignatureTokenCache { get; } = new();
    private readonly Dictionary<(AssemblyId, ModuleId), MdMemberRef> _callbackReferences = new();

    protected override HResult Initialize(int iCorProfilerInfoVersion)
    {
        Logger.Info($"Profiler version: {iCorProfilerInfoVersion}");

        if (!TryLoadEnvVariables())
        {
            return HResult.E_FAIL;
        }

        LoadPdbFiles();

        if (iCorProfilerInfoVersion < 11)
        {
            Logger.Error($"COR profiler version is not supported (min version is 11): {iCorProfilerInfoVersion}");
            return HResult.E_FAIL;
        }

        const CorPrfMonitor eventMask = CorPrfMonitor.COR_PRF_MONITOR_JIT_COMPILATION |
                                        CorPrfMonitor.COR_PRF_MONITOR_MODULE_LOADS |
                                        CorPrfMonitor.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                                        CorPrfMonitor.COR_PRF_DISABLE_INLINING;


        var hSetEventMask = ICorProfilerInfo11.SetEventMask(eventMask);
        return hSetEventMask;
    }

    protected override HResult ModuleLoadFinished(ModuleId moduleId, HResult hrStatus)
    {
        var (hGetModule, modulePath, _, assemblyId) = ICorProfilerInfo2.GetModuleInfo(moduleId);
        
        if (!hGetModule.IsOK)
        {
            Logger.Error($"Failed to get module info for moduleId id {moduleId.Value}: {hGetModule.Code}");
            return HResult.S_OK;
        }
        
        var moduleName = Path.GetFileNameWithoutExtension(modulePath);
        var symbolFileExists = CallbackCache.Instance.TryGetModuleByName(moduleName, out var module);

        if (!symbolFileExists || module is null) return HResult.S_OK;

        var (hGetModuleMetadata, metadataImport) = ICorProfilerInfo2.GetModuleMetaData(
            moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);

        if (!hGetModuleMetadata.IsOK)
        {
            Logger.Error($"Failed to get module metadata for module id {moduleId.Value}: {hGetModuleMetadata.Code}");
            return HResult.S_OK;
        }

        var getMetadataEmit = metadataImport.QueryInterface(KnownGuids.IMetaDataEmit, out var emitPtr);

        if (!getMetadataEmit.IsOK)
        {
            Logger.Error($"Failed to get metadata emit for module {moduleId}");
            return HResult.S_OK;
        }

        var metadataEmit = new IMetadataEmitInvoker(emitPtr);
        var getAssemblyEmit = metadataImport.QueryInterface(KnownGuids.IMetaDataAssemblyEmit, out var assemblyEmitPtr);

        if (!getAssemblyEmit.IsOK)
        {
            Logger.Error($"Failed to get metadata assembly emit for module {moduleId}");
            return HResult.S_OK;
        }

        var metadataAssemblyEmit = new IMetaDataAssemblyEmitInvoker(assemblyEmitPtr);
        
        // References LineProfiler.Shims
        MdAssemblyRef assemblyRef;
        byte[] publicKeyToken = [0xF6, 0x02, 0xDE, 0x21, 0xE5, 0xAC, 0xFB, 0x2D];
        fixed (byte* pKey = publicKeyToken)
        fixed (char* pName = "LineProfiler.Shims\0")
        {
            var asmMeta = new ASSEMBLYMETADATA
            {
                usMajorVersion = 1,
                usMinorVersion = 0,
                usBuildNumber = 0,
                usRevisionNumber = 0,
                szLocale = null,
                cbLocale = 0,
                rdwProcessor = null,
                ulProcessor = 0,
                rOS = null,
                ulOS = 0
            };
            
            var hDefineAssembly = metadataAssemblyEmit.DefineAssemblyRef(
                pKey, 8,
                pName,
                &asmMeta,
                null,
                0,
                0,
                out assemblyRef);

            if (!hDefineAssembly.IsOK)
            {
                Logger.Error($"Failed to define assembly ref ({hDefineAssembly.Code}), {hDefineAssembly.ToString()}");
                return HResult.S_OK;
            }
        }
        
        // Add type reference for LineProfiler.Shims.CallbackShim
        MdTypeRef typeRef;
        fixed (char* typeName = "LineProfiler.Shims.Callbacks\0")
        {
            var hDefineTypeRef = metadataEmit.DefineTypeRefByName(assemblyRef.Value, typeName, out typeRef);

            if (!hDefineTypeRef.IsOK)
            {
                Logger.Error($"Failed to define type ref ({hDefineTypeRef.Code}), {hDefineTypeRef.ToString()}");
                return HResult.S_OK;
            }
        }
        
        // Add member reference for LineProfiler.Shims.CallbackShim.Method
        const string memberName = "ProcessProbes\0";

        fixed (byte* pSig = Callbacks.Signature.BinaryData)
        {
            fixed (char* pMemberName = memberName)
            {
                var hDefineMember = metadataEmit.DefineMemberRef((uint)typeRef.Value, pMemberName, pSig,
                    (uint) Callbacks.Signature.BinaryData.Length,
                    out var memberRef);

                if (!hDefineMember.IsOK)
                {
                    Logger.Error($"Failed to define member ref: {hDefineMember.Code}, {hDefineMember.ToString()}");
                    return HResult.S_OK;
                }

                _callbackReferences[(assemblyId, moduleId)] = memberRef;
            }
        }

        return HResult.S_OK;
    }

    protected override HResult JITCompilationStarted(FunctionId functionId, bool fIsSafeToBlock)
    {
        var (hGetFunction, _, moduleId, methodToken) = ICorProfilerInfo2.GetFunctionInfo(functionId);

        if (!hGetFunction.IsOK)
        {
            Logger.Error($"Failed to get function info for function id {functionId.Value}: {hGetFunction.Code}");
            return HResult.S_OK;
        }

        var (hGetModule, modulePath, _, assemblyId) = ICorProfilerInfo2.GetModuleInfo(moduleId);

        if (!hGetModule.IsOK)
        {
            Logger.Error($"Failed to get module info for moduleId id {moduleId.Value}: {hGetModule.Code}");
            return HResult.S_OK;
        }

        var moduleName = Path.GetFileNameWithoutExtension(modulePath);
        var symbolFileExists = CallbackCache.Instance.TryGetModuleByName(moduleName, out var module);
        if (!symbolFileExists || module is null) return HResult.S_OK;

        if (!module.TryGetFunctionByToken(methodToken.Value, out var pdbFunction) || pdbFunction is null)
        {
            Logger.Error($"Failed to get method debug info for method token {methodToken.Value}");
            return HResult.S_OK;
        }
        
        if (!_callbackReferences.TryGetValue((assemblyId, moduleId), out var callbackReference))
        {
            Logger.Error($"Member ref for shim not initialized for module id {moduleId} in assembly {assemblyId}");
            return HResult.S_OK;
        }

        // Get method name and type name
        var (hGetModuleMetadata, metaDataImport) = ICorProfilerInfo2.GetModuleMetaData(
            moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);

        if (!hGetModuleMetadata.IsOK)
        {
            Logger.Error($"Failed to get module metadata for module id {moduleId.Value}: {hGetModuleMetadata.Code}");
            return HResult.S_OK;
        }

        // Get method name
        var methodNameSpan = UnsafeTypes.CreateSpanForName();
        var mdMethodToken = new MdMethodDef(methodToken);

        var hGetMethodProps = metaDataImport.GetMethodProps(
            mdMethodToken, out var typeToken, methodNameSpan, out var cchMethod,
            out _, out _, out _, out _, out _);

        if (!hGetMethodProps.IsOK)
        {
            Logger.Error(
                $"Failed to get method props for method with token {methodToken.Value}: {hGetMethodProps.Code}");
            return HResult.S_OK;
        }

        var methodName = UnsafeTypes.ConvertSpanToString(methodNameSpan, cchMethod);

        // Get type name
        var typeNameSpan = UnsafeTypes.CreateSpanForName();
        var hGetTypeProps = metaDataImport.GetTypeDefProps(typeToken, typeNameSpan, out var cchType, out _, out _);

        if (!hGetTypeProps.IsOK)
        {
            Logger.Error($"Failed to get type props for type with token {typeToken.Value}: {hGetTypeProps.Code}");
            return HResult.S_OK;
        }

        var typeName = UnsafeTypes.ConvertSpanToString(typeNameSpan, cchType);
        
        // Only instrument exact benchmark
        var isBenchMark = typeName.StartsWith("LineProfiler.Benchmarks");
        if (isBenchMark && !methodName.Equals("Method", StringComparison.InvariantCultureIgnoreCase))
        {
            Logger.Info($"Not intercepting benchmark method {typeName}.{methodName}");
            return HResult.S_OK;
        }
        
        // Cache method and type name
        var lines = pdbFunction.SequencePoints.SelectMany(sp => sp.LineNumbers).ToArray();
        var startLine = lines.Length > 0 ? lines.Min() : 0;
        var endLine = lines.Length > 0 ? lines.Max() : 0;
        
        CallbackCache.Instance.Metadata[methodToken.Value] = 
            new MethodMetadata(methodName, typeName, startLine, endLine);

        // Read method body
        var hGetFunctionBody =
            ICorProfilerInfo2.GetILFunctionBody(moduleId, mdMethodToken, out var pMethodHeader, out var methodSize);

        if (!hGetFunctionBody.IsOK)
        {
            Logger.Error($"Failed to instrument: Failed to get IL function body: {hGetFunctionBody.Code}");
            return HResult.S_OK;
        }

        var stream = UnsafeTypes.GetStreamFromBytePtr(pMethodHeader, methodSize);
        var reader = new MethodReader(stream);
        var method = reader.ReadFromStream();

        // Instrument method
        Logger.Info($"Instrumenting {typeName}.{methodName} for interception");

        try
        {
            var instrumentedMethod = InstrumentMethod(
                method, pdbFunction, assemblyId, methodToken, callbackReference, metaDataImport);
            
            var instrumentedMethodBytes = ConvertMethodToByteArray(instrumentedMethod);
            var pInstrumentedMethod = UnsafeTypes.GetBytePtr(instrumentedMethodBytes);
            
            MethodPrinter.PrintMethodBody(instrumentedMethod.Body);
            CallbackCache.UpdatePointer();
            var hSetBody = ICorProfilerInfo2.SetILFunctionBody(moduleId, mdMethodToken, pInstrumentedMethod);

            if (!hSetBody.IsOK)
            {
                Logger.Error($"Failed to instrument: Failed to set IL function body: {hSetBody.Code}");
                return HResult.S_OK;
            }
            
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to instrument method: {ex.Message}");
            Logger.Error($"Failed to instrument method: {ex.StackTrace}");
            return HResult.S_OK;
        }

        return HResult.S_OK;
    }

    protected override HResult Shutdown()
    {
        Logger.Info("Shutting down...");
        return HResult.S_OK;
    }

    private static bool TryLoadEnvVariables()
    {
        var pdbDirectory = Environment.GetEnvironmentVariable(EnvVars.PdbDirectory.Name);

        if (string.IsNullOrEmpty(pdbDirectory) || !Directory.Exists(pdbDirectory))
        {
            Logger.Error($"Failed to load environment variable '{EnvVars.PdbDirectory.Name}'");
            return false;
        }

        EnvVars.PdbDirectory.Value = pdbDirectory;
        return true;
    }

    private static void LoadPdbFiles()
    {
        var symbolFiles = GetPdbFilesInDirectory(EnvVars.PdbDirectory.Value);
        CallbackCache.Instance.CachePdbFiles(symbolFiles);
        CallbackCache.UpdatePointer();
    }

    private static string[] GetPdbFilesInDirectory(string directory)
    {
        var pdbDirFiles = Directory.GetFiles(directory);
        var pdbFiles = pdbDirFiles
            .Where(file =>
            {
                // Only load pdb files
                var fileExtension = Path.GetExtension(file);
                if (fileExtension != ".pdb") return false;
                // Exclude the profiler itself
                var fileName = Path.GetFileNameWithoutExtension(file);
                return !fileName.StartsWith("Profiler.Core") && 
                       !fileName.Equals("LineProfiler.Shims");
            })
            .ToArray();
        return pdbFiles;
    }

    private Method InstrumentMethod(
        Method method,
        PdbFunction symbols,
        AssemblyId assemblyId,
        MdToken methodToken,
        MdMemberRef callbackRef,
        MetaDataImport metaDataImport)
    {
        // Find type references in assembly
        var boolRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Boolean", metaDataImport);

        // Patch local variable signature
        var varSignature = LocalVarSignatureReader.ReadLocalVarSignature(method.Header, metaDataImport);
        var patchedVarSignature = MethodInstrumentation.CreateLocalVarSignature(varSignature);

        // Emit necessary signatures into assembly and get their tokens
        var metadataEmit = metaDataImport.GetMetadataEmit();
        
        var mdLocalVarSignature =
            SignatureTokenCache.GetMdSignature(assemblyId, patchedVarSignature.BinaryData, metadataEmit);
        
        var localVarCount = patchedVarSignature.GetVarCount();

        // Places probes
        var metadata = new InstrumentationMetadata(
            methodToken, boolRef, callbackRef, mdLocalVarSignature, localVarCount);

        var originalInstructionCount = method.Body.Instructions.Count;
        var instrumentedMethod = MethodInstrumentation.InstrumentMethod(method, symbols, metadata);

        Logger.Info(
            $"Instrumented method instruction count: {instrumentedMethod.Body.Instructions.Count} " +
            $"(originally: {originalInstructionCount})");

        return instrumentedMethod;
    }
    
    private static byte[] ConvertMethodToByteArray(Method method)
    {
        var memoryStream = new MemoryStream();
        var methodWriter = new MethodWriter();
        methodWriter.WriteToStream(method, memoryStream);
        return memoryStream.ToArray();
    }
}