using System.Collections.Concurrent;
using System.Net;
using System.Text;
using IlRewriter.Models;
using IlRewriter.Readers.Method;
using IlRewriter.Writers;
using Profiler.Core;
using Profiler.Sample.IL;
using Profiler.Sample.IL.Interfaces;

namespace Profiler.Sample;

internal unsafe class CorProfiler : CorProfilerCallback10Base
{
    private static ConcurrentQueue<string> Logs { get; } = new();

    private InMemoryTypeRefCache TypeRefCache { get; } = new InMemoryTypeRefCache();
    
    private InMemorySignatureTokenCache SignatureTokenCache { get; } = new InMemorySignatureTokenCache();
    
    private LocalSignatureReader LocalSignatureReader { get; } = new LocalSignatureReader();
    
    private readonly byte[] _probeSignature = MethodSignatureFactory.Create(
        CorUnmanagedCallingConventionFlags.IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL, CorElementTypeFlags.ELEMENT_TYPE_VOID, CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN, CorElementTypeFlags.ELEMENT_TYPE_I4);
    
    private Dictionary<IntPtr, int> _probeSigTokens = new Dictionary<IntPtr, int>();

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
        ICorProfilerInfo2.GetAssemblyInfo(assemblyId, assemblyNameSpan, out var cchAssembly, out var _, out var _);
        var assemblyName = TypeHelper.ConvertSpanToString(assemblyNameSpan, cchAssembly);

        if (!ShouldInterceptAssembly(assemblyName))
        {
            return HResult.S_OK;
        }
        
        // Get method name and type name
        var (_, metaDataImport) = ICorProfilerInfo2.GetModuleMetaData(moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, KnownGuids.IMetaDataImport);
        
        // Get method name
        var methodNameSpan = TypeHelper.CreateSpanForName();
        var mdMethodToken = new MdMethodDef(methodToken);
        
        metaDataImport.GetMethodProps(mdMethodToken, out var typeToken, methodNameSpan, out var cchMethod, out _, out _, out _, out _, out _);
        var methodName = TypeHelper.ConvertSpanToString(methodNameSpan, cchMethod);

        // Get type name
        var typeNameSpan = TypeHelper.CreateSpanForName();
        metaDataImport.GetTypeDefProps(typeToken, typeNameSpan, out var cchType, out var _, out var _);
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
        Console.WriteLine($"Instrumenting {typeName}.{methodName} for interception");

        var mdBoolRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Boolean", metaDataImport);
        var mdActivityRef = TypeRefCache.GetMdTypeRef(assemblyId, "System.Diagnostics.Activity", metaDataImport);
        Log($"System.Boolean type ref: {mdBoolRef.Value}");
        Log($"System.Diagnostics.Activity type ref: {mdActivityRef.Value}");
        
            
        // Get updated local signature
        var localSignature = LocalSignatureReader.ReadLocalSignature(method.Header, metaDataImport);
        Log($"Local signature before: {BitConverter.ToString(localSignature.Data)}");
        
        byte[] pinnedBoolArraySignature = [CorElementTypeFlags.ELEMENT_TYPE_PINNED, CorElementTypeFlags.ELEMENT_TYPE_SZARRAY, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        byte[] boolPtrSignature = [CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN];
        localSignature.AddLocalVariable(pinnedBoolArraySignature);
        localSignature.AddLocalVariable(boolPtrSignature);
        Log($"Local signature: {BitConverter.ToString(localSignature.Data)}");
        
        var metaDataEmit = metaDataImport.GetMetadataEmit();
        var mdLocalSignature = SignatureTokenCache.GetMdSignature(assemblyId, localSignature.Data, metaDataEmit);
        var mdProbeSignature = SignatureTokenCache.GetMdSignature(assemblyId, _probeSignature, metaDataEmit);
            
        // Patch method
        var localsCount = localSignature.GetLocalsCount();
        var patchedMethod = MethodInstrumentation.PatchMethod(method, mdBoolRef, localsCount - 1, mdLocalSignature, mdProbeSignature);
        var patchedMethodBytes = ConvertMethodToByteArray(patchedMethod);
        var patchedMethodPointer = TypeHelper.GetBytePtr(patchedMethodBytes);
        
        var hSetBody = ICorProfilerInfo2.SetILFunctionBody(moduleId, mdMethodToken, patchedMethodPointer);
        
        if (!hSetBody.IsOK)
        {
            Log($"Failed to instrument: Failed to set IL function body: {hSetBody.Code}");
            return HResult.S_OK;
        }
        
        Log("");
        return HResult.S_OK;
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
        //Logs.Enqueue(line);
    }
}
