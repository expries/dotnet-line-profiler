using System.Collections.Concurrent;
using System.Text;
using IlRewriter;
using IlRewriter.Models;
using IlRewriter.Models.Signatures;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using LineProfiler.Caches;
using LineProfiler.Helpers;
using LineProfiler.Instrumentation;
using NativeObjects;
using Profiler.Core;

namespace LineProfiler;

internal unsafe class CorProfiler : CorProfilerCallback10Base
{
    private static ConcurrentQueue<string> Logs { get; } = new();

    private TypeRefCache TypeRefCache { get; } = new();

    private SignatureTokenCache SignatureTokenCache { get; } = new();

    protected override HResult Initialize(int iCorProfilerInfoVersion)
    {
        Log($"Profiler version: {iCorProfilerInfoVersion}");

        if (iCorProfilerInfoVersion < 11)
        {
            return HResult.E_FAIL;
        }

        const CorPrfMonitor eventMask = CorPrfMonitor.COR_PRF_MONITOR_JIT_COMPILATION |
                                        CorPrfMonitor.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                                        CorPrfMonitor.COR_PRF_DISABLE_INLINING;

        var hSetEventMask = ICorProfilerInfo11.SetEventMask(eventMask);

        const string directoryWithPdbFiles =
            "C:\\Coding\\otel-line-profiler\\profiler\\SampleConsoleApp\\obj\\Debug\\net8.0";
        var pdbFiles = ScanDirectoryForPdbFiles(directoryWithPdbFiles);
        PdbCache.Initialize(pdbFiles);

        return hSetEventMask;
    }

    private static string[] ScanDirectoryForPdbFiles(string directory)
    {
        var pdbDirFiles = Directory.GetFiles(directory);
        var pdbFiles = pdbDirFiles
            .Where(file => Path.GetExtension(file).Equals(".pdb"))
            .ToArray();
        return pdbFiles;
    }

    protected override HResult JITCompilationStarted(FunctionId functionId, bool fIsSafeToBlock)
    {
        var (hGetFunction, _, moduleId, methodToken) = ICorProfilerInfo2.GetFunctionInfo(functionId);

        if (!hGetFunction.IsOK)
        {
            Log($"Failed to get function info for function id {functionId.Value}: {hGetFunction.Code}");
            return HResult.S_OK;
        }

        var (hGetModule, modulePath, _, assemblyId) = ICorProfilerInfo2.GetModuleInfo(moduleId);

        if (!hGetModule.IsOK)
        {
            Log($"Failed to get module info for moduleId id {moduleId.Value}: {hGetModule.Code}");
            return HResult.S_OK;
        }

        var moduleName = Path.GetFileNameWithoutExtension(modulePath);
        var symbolFileExists = PdbCache.TryGetModuleByName(moduleName, out var module);

        if (!symbolFileExists || module is null)
        {
            return HResult.S_OK;
        }

        PdbCache.AddModuleAlias(assemblyId, moduleName);
        module.AddFunctionAlias(methodToken.Value, functionId.Value);

        // Get method name and type name
        var (hGetModuleMetadata, metaDataImport) = ICorProfilerInfo2.GetModuleMetaData(
            moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);

        if (!hGetModuleMetadata.IsOK)
        {
            Log($"Failed to get module metadata for module id {moduleId.Value}: {hGetModuleMetadata.Code}");
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
            Log($"Failed to get method props for method with token {methodToken.Value}: {hGetMethodProps.Code}");
            return HResult.S_OK;
        }

        var methodName = UnsafeTypes.ConvertSpanToString(methodNameSpan, cchMethod);

        // Get type name
        var typeNameSpan = UnsafeTypes.CreateSpanForName();
        var hGetTypeProps = metaDataImport.GetTypeDefProps(typeToken, typeNameSpan, out var cchType, out _, out _);

        if (!hGetTypeProps.IsOK)
        {
            Log($"Failed to get type props for type with token {typeToken.Value}: {hGetTypeProps.Code}");
            return HResult.S_OK;
        }

        var typeName = UnsafeTypes.ConvertSpanToString(typeNameSpan, cchType);

        // Read method body
        var hGetFunctionBody =
            ICorProfilerInfo2.GetILFunctionBody(moduleId, mdMethodToken, out var pMethodHeader, out var methodSize);

        if (!hGetFunctionBody.IsOK)
        {
            Log($"Failed to instrument: Failed to get IL function body: {hGetFunctionBody.Code}");
            return HResult.S_OK;
        }

        /*
        var bArr = UnsafeTypes.GetByteArrayFromBytePtr(pMethodHeader, methodSize);
        var fileName = $"{assemblyId.Value}_{functionId.Value}";
        File.WriteAllBytes($@"C:\Coding\otel-line-profiler\profiler\Release\{fileName}", bArr);
        */
        
        var stream = UnsafeTypes.GetStreamFromBytePtr(pMethodHeader, methodSize);
        var reader = new MethodReader(stream);
        var method = reader.ReadFromStream();

        // Instrument method
        Log($"Instrumenting {typeName}.{methodName} for interception");

        try
        {
            var instrumentedMethod = InstrumentMethod(method, assemblyId, functionId, metaDataImport);
            var instrumentedMethodBytes = ConvertMethodToByteArray(instrumentedMethod);
            var pInstrumentedMethod = UnsafeTypes.GetBytePtr(instrumentedMethodBytes);

            var hSetBody = ICorProfilerInfo2.SetILFunctionBody(moduleId, mdMethodToken, pInstrumentedMethod);

            if (!hSetBody.IsOK)
            {
                Log($"Failed to instrument: Failed to set IL function body: {hSetBody.Code}");
                return HResult.S_OK;
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to instrument method: {ex.Message}");
            return HResult.S_OK;
        }

        return HResult.S_OK;
    }

    private Method InstrumentMethod(
        Method method,
        AssemblyId assemblyId,
        FunctionId functionId,
        MetaDataImport metaDataImport)
    {
        // Find type references in assembly
        var boolRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Boolean", metaDataImport);
        var activityRefs = GetActivityTypeReferences(assemblyId, metaDataImport);
        var traceIdRefs = GetTraceIdTypeRefs(activityRefs, metaDataImport);

        // Patch local variable signature
        var varSignature = LocalVarSignatureReader.ReadLocalVarSignature(method.Header, metaDataImport);
        var patchedVarSignature = MethodInstrumentation.CreateLocalVarSignature(
            varSignature, activityRefs.Activity, traceIdRefs.TraceId);

        // Emit necessary signatures into assembly and get their tokens
        var metadataEmit = metaDataImport.GetMetadataEmit();
        var signatureTokens = GetAndCacheSignatureTokens(
            assemblyId, patchedVarSignature, Callbacks.Signature, metadataEmit);
        var localVarCount = patchedVarSignature.GetVarCount();
        
        var metadata = new InstrumentationMetadata(
            assemblyId, functionId, boolRef, activityRefs, traceIdRefs, signatureTokens, localVarCount);

        // Collect probes
        var originalInstructionCount = method.Body.Instructions.Count;
        var instrumentedMethod = MethodInstrumentation.InstrumentMethod(method, metadata);

        Console.WriteLine(
            $"Instrumented method instruction count: {instrumentedMethod.Body.Instructions.Count} " +
            $"(originally: {originalInstructionCount})");

        return instrumentedMethod;
    }

    private SignatureTokens GetAndCacheSignatureTokens(
        AssemblyId assemblyId, 
        LocalVarSignature localVarSignature,
        MethodSignature callbackSignature,
        IMetadataEmitInvoker metaDataEmit)
    {
        // Get signature tokens
        var mdLocalSignature =
            SignatureTokenCache.GetMdSignature(assemblyId, localVarSignature.BinaryData, metaDataEmit);
        
        var mdProbeSignature =
            SignatureTokenCache.GetMdSignature(assemblyId, callbackSignature.BinaryData, metaDataEmit);

        return new SignatureTokens
        {
            Callback = mdProbeSignature,
            LocalVars = mdLocalSignature,
        };
    }

    private ActivityTypeRefs GetActivityTypeReferences(AssemblyId assemblyId, MetaDataImport metaDataImport)
    {
        var activityRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Diagnostics.Activity", metaDataImport);
        
        var (mCurrentActivity, _) = TypeRefCache
            .GetMemberRef(activityRef, "get_Current", metaDataImport);
        
        var (mGetTraceId, mGetTraceIdSignatureBytes) = TypeRefCache
            .GetMemberRef(activityRef, "get_TraceId", metaDataImport);
        
        var mGetTraceIdSignature = new MemberRefSignature(mGetTraceIdSignatureBytes);

        return new ActivityTypeRefs(mGetTraceIdSignature)
        {
            Activity = activityRef,
            Current = mCurrentActivity,
            GetTraceId = mGetTraceId
        };
    }

    private static TraceIdTypeRefs GetTraceIdTypeRefs(ActivityTypeRefs activityTypeRefs, MetaDataImport metaDataImport)
    {
        if (!activityTypeRefs.GetTraceIdSignature.TryParseReturnType(out var traceIdRid))
        {
            throw new Exception($"Failed to parse member's return type '{activityTypeRefs.GetTraceIdSignature}'");
        }

        var traceIdRef = new MdTypeRef(traceIdRid.ToTypeRefToken());
        
        var (mToHexString, _) = TypeRefCache
            .GetMemberRef(traceIdRef, "ToHexString", metaDataImport);
        
        return new TraceIdTypeRefs
        {
            TraceId = traceIdRef,
            ToHexString = mToHexString
        };
    }



    protected override HResult Shutdown()
    {
        var logFile = new FileStream(@"C:\Users\Samuel\Desktop\logs.txt", FileMode.OpenOrCreate);
        Console.WriteLine("[Profiler] *** Shutting down, dumping remaining logs ***");

        while (Logs.TryDequeue(out var log))
        {
            Console.WriteLine($"[Profiler] {log}");
            logFile.Write(Encoding.UTF8.GetBytes($"[Profiler] {log}\n"));
        }

        logFile.Close();
        return HResult.S_OK;
    }

    private static byte[] ConvertMethodToByteArray(Method method)
    {
        var memoryStream = new MemoryStream();
        var methodWriter = new MethodWriter();
        methodWriter.WriteToStream(method, memoryStream);
        return memoryStream.ToArray();
    }

    private static void Log(string line)
    {
        Logs.Enqueue(line);
    }
}