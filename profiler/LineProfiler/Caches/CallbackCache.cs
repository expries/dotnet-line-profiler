using System.Runtime.InteropServices;
using LineProfiler.Configuration;
using LineProfiler.DebugInfo;

namespace LineProfiler.Caches;

public class ProbeCacheEntry(
    string blockId,
    int[] coveredSourceLines,
    int[] sourceLinesInBlock)
{
    public string BlockId { get; set; } = blockId;
    public int[] CoveredSourceLines { get; set; } = coveredSourceLines;
    public int[] SourceLinesInBlock { get; set; } = sourceLinesInBlock;
}

public class CallbackCache(
    Dictionary<int, Dictionary<int, ProbeCacheEntry>> probes,
    Dictionary<int, PdbSourceCodeFile> sourceFile,
    Dictionary<int, MethodMetadata> metadata)
    : IDisposable
{
    public static readonly CallbackCache Instance = new(
        new Dictionary<int, Dictionary<int, ProbeCacheEntry>>(),
        new Dictionary<int, PdbSourceCodeFile>(),
        new Dictionary<int, MethodMetadata>());

    public GCHandle Handle;
    public GCHandle SizeHandle;

    public Dictionary<int, Dictionary<int, ProbeCacheEntry>> Probes = probes;
    public Dictionary<int, PdbSourceCodeFile> SourceFile = sourceFile;
    public Dictionary<int, MethodMetadata> Metadata = metadata;

    private readonly Dictionary<string, PdbModule> ModulesByName = new();

    public static void UpdatePointer()
    {
        lock (Instance)
        {
            Console.WriteLine("Serializing " + Instance.Probes.Count + " probe entries");
            var serialized = CacheSerializer.Serialize(Instance);
            Instance.Handle = UpdateHandleTarget(Instance.Handle, serialized);
            Instance.SizeHandle = UpdateHandleTarget(Instance.SizeHandle, serialized.Length);
        }
    }

    private static GCHandle UpdateHandleTarget(GCHandle handle, object? target)
    {
        var updatedHandle = handle;

        if (handle.IsAllocated)
        {
            handle.Target = target;
        }
        else
        {
            updatedHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        return updatedHandle;
    }

    public static CallbackCache CreateFromByteArray(byte[] serialized)
    {
        return CacheSerializer.Deserialize(serialized);
    }

    public void CachePdbFiles(string[] pdbFilePaths)
    {
        foreach (var pdbPath in pdbFilePaths)
        {
            try
            {
                CachePdb(pdbPath);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load pdb file from path '{pdbPath}': {e.Message}");
            }
        }
    }

    public bool TryGetModuleByName(string moduleName, out PdbModule? module)
    {
        return ModulesByName.TryGetValue(moduleName, out module);
    }

    private void CachePdb(string pdbPath)
    {
        var moduleName = Path.GetFileNameWithoutExtension(pdbPath);
        ModulesByName[moduleName] = PdbParser.ParsePdbModule(pdbPath);
    }

    public void Dispose()
    {
        Handle.Free();
    }
}