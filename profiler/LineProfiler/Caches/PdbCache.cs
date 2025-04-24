using LineProfiler.DebugInfo;
using Profiler.Core;

namespace LineProfiler.Caches;

public static class PdbCache
{
    private static readonly Dictionary<string, PdbModule> ModulesByName = new ();
    private static readonly Dictionary<AssemblyId, PdbModule> ModulesByAssemblyId = new();

    public static void Initialize(string[] pdbFilePaths)
    {
        foreach (var pdbPath in pdbFilePaths)
        {
            CachePdb(pdbPath);
        }
    }

    public static void AddModuleAlias(AssemblyId assemblyId, string moduleName)
    {
        if (ModulesByName.TryGetValue(moduleName, out var module))
        {
            ModulesByAssemblyId[assemblyId] = module;
        }
    }

    public static bool TryGetModule(string moduleName, out PdbModule? module)
    {
        return ModulesByName.TryGetValue(moduleName, out module);
    }
    
    public static bool TryGetModule(AssemblyId assemblyId, out PdbModule? module)
    {
        return ModulesByAssemblyId.TryGetValue(assemblyId, out module);
    }

    private static void CachePdb(string pdbPath)
    {
        var moduleName = Path.GetFileNameWithoutExtension(pdbPath);
        ModulesByName[moduleName] = PdbParser.ParsePdbModule(pdbPath);
        Console.WriteLine($"Caching module {moduleName}");
    }
}