using System.Collections.Concurrent;
using System.Text;
using IlRewriter.Models;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using OtelLineProfiler.Caches;
using OtelLineProfiler.Helpers;
using OtelLineProfiler.IL;
using Profiler.Core;

namespace OtelLineProfiler;

internal unsafe class CorProfiler : CorProfilerCallback10Base
{
    private static ConcurrentQueue<string> Logs { get; } = new();

    private InMemoryTypeRefCache TypeRefCache { get; } = new();
    
    private InMemorySignatureTokenCache SignatureTokenCache { get; } = new();
    
    private LocalVarVarSignatureReader LocalVarVarSignatureReader { get; } = new();
    

    
    private Dictionary<IntPtr, int> _probeSigTokens = new();

    protected override HResult Initialize(int iCorProfilerInfoVersion)
    {
        Log("Initialize");
        Log($"Profiler version {iCorProfilerInfoVersion}");

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
        var (_, _, moduleId, methodToken) = ICorProfilerInfo2.GetFunctionInfo(functionId);
        var (_, _, _, assemblyId) = ICorProfilerInfo2.GetModuleInfo(moduleId);
        
        // Check if assembly of method is the one we want to intercept
        var assemblyNameSpan = TypeHelper.CreateSpanForName();
        var hGetAssembly = ICorProfilerInfo2.GetAssemblyInfo(assemblyId, assemblyNameSpan, out var cchAssembly, out var _, out var _);

        if (!hGetAssembly.IsOK)
        {
            Log($"Failed to get assembly info for assembly id {assemblyId}");
            return HResult.S_OK;
        }
        
        var assemblyName = TypeHelper.ConvertSpanToString(assemblyNameSpan, cchAssembly);

        if (!ShouldInterceptAssembly(assemblyName))
        {
            return HResult.S_OK;
        }
        
        // Get method name and type name
        var (hGetModuleMetadata, metaDataImport) = ICorProfilerInfo2.GetModuleMetaData(moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);
        
        if (!hGetModuleMetadata.IsOK)
        {
            Log($"Failed to get module metadata for module id {moduleId}");
            return HResult.S_OK;
        }
        
        // Get method name
        var methodNameSpan = TypeHelper.CreateSpanForName();
        var mdMethodToken = new MdMethodDef(methodToken);
        
        var hGetMethodProps = metaDataImport.GetMethodProps(mdMethodToken, out var typeToken, methodNameSpan, out var cchMethod, out _, out _, out _, out _, out _);

        if (!hGetMethodProps.IsOK)
        {
            Log($"Failed to get method props for method with token {methodToken.Value}");
            return HResult.S_OK;
        }
        
        var methodName = TypeHelper.ConvertSpanToString(methodNameSpan, cchMethod);

        // Get type name
        var typeNameSpan = TypeHelper.CreateSpanForName();
        var hGetTypeProps = metaDataImport.GetTypeDefProps(typeToken, typeNameSpan, out var cchType, out var _, out var _);

        if (!hGetTypeProps.IsOK)
        {
            Log($"Failed to get type props for type with token {typeToken.Value}");
            return HResult.S_OK;
        }
        
        var typeName = TypeHelper.ConvertSpanToString(typeNameSpan, cchType);
        
        if (!ShouldInterceptMethod(typeName, methodName)) return HResult.S_OK;
        
        // Read method body
        var hGetFunctionBody = ICorProfilerInfo2.GetILFunctionBody(moduleId, mdMethodToken, out var pMethodHeader, out var methodSize);

        if (!hGetFunctionBody.IsOK)
        {
            Log($"Failed to instrument: Failed to get IL function body: {hGetFunctionBody.Code}");
            return HResult.S_OK;
        }
        
        var stream = TypeHelper.GetStreamFromBytePointer(pMethodHeader, methodSize);
        var reader = new MethodReader(stream);
        var method = reader.ReadFromStream();
        
        // Instrument method
        Log($"Instrumenting {typeName}.{methodName} for interception");
        
        var instrumentedMethod = InstrumentMethod(method, assemblyId, metaDataImport);
        var methodBytes = ConvertMethodToByteArray(instrumentedMethod);
        var methodBytePointer = TypeHelper.GetBytePtr(methodBytes);
        
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
        var mdBoolRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Boolean", metaDataImport);
        //var mdActivityRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Diagnostics.Activity", metaDataImport);
        Log($"System.Boolean type ref: {mdBoolRef.Value}");
        //Log($"System.Diagnostics.Activity type ref: {mdActivityRef.Value}");
            
        // Get updated local signature
        var localSignature = LocalVarVarSignatureReader.ReadLocalVarSignature(method.Header, metaDataImport);
        localSignature = MethodFlowInstrumentation.PatchLocalVariableSignature(localSignature);
        Log($"Local signature before: {BitConverter.ToString(localSignature.Data)}");
        Log($"Local signature: {BitConverter.ToString(localSignature.Data)}");

        var metaDataEmit = metaDataImport.GetMetadataEmit();
        var mdLocalSignature = SignatureTokenCache.GetMdSignature(assemblyId, localSignature.Data, metaDataEmit);
        var mdProbeSignature = SignatureTokenCache.GetMdSignature(assemblyId, MethodFlowInstrumentation.ProbeSignature, metaDataEmit);
            
        // Patch method
        var localVarCount = localSignature.GetVarCount();
        var instrumentedMethod = MethodFlowInstrumentation.InstrumentMethod(method, localVarCount, mdBoolRef, mdLocalSignature, mdProbeSignature);
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
