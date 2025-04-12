using System.Collections.Concurrent;
using System.Text;
using IlRewriter.Models;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using LineProfiler.Caches;
using LineProfiler.Helpers;
using LineProfiler.IL;
using Profiler.Core;

namespace LineProfiler;

internal unsafe class CorProfiler : CorProfilerCallback10Base
{
    private static ConcurrentQueue<string> Logs { get; } = new();

    private InMemoryTypeRefCache TypeRefCache { get; } = new();
    
    private InMemorySignatureTokenCache SignatureTokenCache { get; } = new();
    
    private LocalVarSignatureReader LocalVarSignatureReader { get; } = new();

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

        return ICorProfilerInfo11.SetEventMask(eventMask);
    }
    
    protected override HResult JITCompilationStarted(FunctionId functionId, bool fIsSafeToBlock)
    {
        var (hGetFunction, _, moduleId, methodToken) = ICorProfilerInfo2.GetFunctionInfo(functionId);

        if (!hGetFunction.IsOK)
        {
            Log($"Failed to get function info for function id {functionId.Value}: {hGetFunction.Code}");
            return HResult.S_OK;
        }
        
        var (hGetModule, _, _, assemblyId) = ICorProfilerInfo2.GetModuleInfo(moduleId);

        if (!hGetModule.IsOK)
        {
            Log($"Failed to get module info for moduleId id {moduleId.Value}: {hGetModule.Code}");
            return HResult.S_OK;
        }
        
        // Check if assembly of method is the one we want to intercept
        var assemblyNameSpan = UnsafeTypes.CreateSpanForName();
        var hGetAssembly = ICorProfilerInfo2.GetAssemblyInfo(assemblyId, assemblyNameSpan, out var cchAssembly, out _, out _);

        if (!hGetAssembly.IsOK)
        {
            Log($"Failed to get assembly info for assembly id {assemblyId.Value}: {hGetAssembly.Code}");
            return HResult.S_OK;
        }
        
        var assemblyName = UnsafeTypes.ConvertSpanToString(assemblyNameSpan, cchAssembly);

        if (!ShouldInterceptAssembly(assemblyName))
        {
            return HResult.S_OK;
        }
        
        // Get method name and type name
        var (hGetModuleMetadata, metaDataImport) = ICorProfilerInfo2.GetModuleMetaData(moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);
        
        if (!hGetModuleMetadata.IsOK)
        {
            Log($"Failed to get module metadata for module id {moduleId.Value}: {hGetModuleMetadata.Code}");
            return HResult.S_OK;
        }
        
        // Get method name
        var methodNameSpan = UnsafeTypes.CreateSpanForName();
        var mdMethodToken = new MdMethodDef(methodToken);
        var hGetMethodProps = metaDataImport.GetMethodProps(mdMethodToken, out var typeToken, methodNameSpan, out var cchMethod, out _, out _, out _, out _, out _);

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
        
        if (!ShouldInterceptMethod(typeName, methodName)) return HResult.S_OK;
        
        // Read method body
        var hGetFunctionBody = ICorProfilerInfo2.GetILFunctionBody(moduleId, mdMethodToken, out var pMethodHeader, out var methodSize);

        if (!hGetFunctionBody.IsOK)
        {
            Log($"Failed to instrument: Failed to get IL function body: {hGetFunctionBody.Code}");
            return HResult.S_OK;
        }
        
        var stream = UnsafeTypes.GetStreamFromBytePtr(pMethodHeader, methodSize);
        var reader = new MethodReader(stream);
        var method = reader.ReadFromStream();
        
        // Instrument method
        Log($"Instrumenting {typeName}.{methodName} for interception");
        
        var instrumentedMethod = InstrumentMethod(method, assemblyId, metaDataImport);
        var methodBytes = ConvertMethodToByteArray(instrumentedMethod);
        var methodBytePointer = UnsafeTypes.GetBytePtr(methodBytes);
        var hSetBody = ICorProfilerInfo2.SetILFunctionBody(moduleId, mdMethodToken, methodBytePointer);
        
        if (!hSetBody.IsOK)
        {
            Log($"Failed to instrument: Failed to set IL function body: {hSetBody.Code}");
            return HResult.S_OK;
        }
        
        Log("");
        return HResult.S_OK;
    }

    private Method InstrumentMethod(Method method, AssemblyId assemblyId, MetaDataImport metaDataImport)
    {
        // Update local var signature for instrumentation
        var varSignature = LocalVarSignatureReader.ReadLocalVarSignature(method.Header, metaDataImport);
        var patchedVarSignature = Instrumentation.CreateLocalVarSignature(varSignature);
        
        // Find bool type token
        var mdBoolRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Boolean", metaDataImport);
        
        // For later
        // var mdActivityRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Diagnostics.Activity", metaDataImport);
        // Log($"System.Diagnostics.Activity type ref: {mdActivityRef.Value}");
        
        // Get signature tokens
        var metaDataEmit = metaDataImport.GetMetadataEmit();
        var mdLocalSignature = SignatureTokenCache.GetMdSignature(assemblyId, patchedVarSignature.Data, metaDataEmit);
        var mdProbeSignature = SignatureTokenCache.GetMdSignature(assemblyId, Instrumentation.ProbeSignature, metaDataEmit);
        
        // Instrument method
        var localVarCount = patchedVarSignature.GetVarCount();
        var instrumentedMethod = Instrumentation.InstrumentMethod(method, localVarCount, mdBoolRef, mdLocalSignature, mdProbeSignature);
        return instrumentedMethod;
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

    private static bool ShouldInterceptAssembly(string assemblyName)
    {
        var isSampleAssembly = assemblyName.StartsWith("Sample", StringComparison.InvariantCultureIgnoreCase);
        return isSampleAssembly;
    }

    private static bool ShouldInterceptMethod(string typeName, string methodName)
    {
        var isMain = methodName.Contains("<Main>", StringComparison.InvariantCultureIgnoreCase);
        var isCtor = methodName.Contains(".ctor", StringComparison.InvariantCultureIgnoreCase) || 
                     methodName.Contains(".cctor", StringComparison.InvariantCultureIgnoreCase);
        return !isMain && !isCtor;
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
        Console.WriteLine(line);
        Logs.Enqueue(line);
    }
}
