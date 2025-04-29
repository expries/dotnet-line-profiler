using LineProfiler.Models;
using Profiler.Core;

namespace LineProfiler.Caches;

public static class ProbeCache
{
    private static readonly Dictionary<(AssemblyId, FunctionId), Dictionary<int, Probe>> Cache = new();

    public static bool TryGetProbes(
        AssemblyId assemblyId,
        FunctionId functionId,
        out Dictionary<int, Probe>? probes)
    {
        return Cache.TryGetValue((assemblyId, functionId), out probes);
    }

    public static void AddMethodProbes(
        AssemblyId assemblyId,
        FunctionId functionId,
        Dictionary<int, Probe> probes)
    {
        Cache[(assemblyId, functionId)] = probes;
    }
}