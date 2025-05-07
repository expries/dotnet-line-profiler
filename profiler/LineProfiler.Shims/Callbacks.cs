using System.Diagnostics;
using System.Runtime.InteropServices;
using LineProfiler.Caches;
using LineProfiler.Configuration;
using LineProfiler.Helpers;
using LineProfiler.Models;
using LineProfiler.Processing;

// ReSharper disable UnusedType.Global

namespace LineProfiler.Shims;

public static class Callbacks
{
    public static unsafe void ProcessProbes(
        int methodToken,
        bool* pProbes,
        int probeLength,
        IntPtr cacheHandlePointer,
        IntPtr cacheSizeHandlePointer)
    {
        var activity = Activity.Current;

        if (activity is null)
        {
            return;
        }

        CallbackCache cache;

        try
        {
            var cacheHandle = GCHandle.FromIntPtr(cacheHandlePointer);
            var cacheSizeHandle = GCHandle.FromIntPtr(cacheSizeHandlePointer);

            var pCache = (byte*)cacheHandle.AddrOfPinnedObject();
            var pCacheSize = (uint*)cacheSizeHandle.AddrOfPinnedObject();
            var cacheSize = *pCacheSize;
            var serializedCache = UnsafeTypes.GetByteArrayFromBytePtr(pCache, cacheSize);
            cache = CallbackCache.CreateFromByteArray(serializedCache);
        }
        catch (Exception exception)
        {
            Logger.Error($"Failed to deserialize callback cache: {exception.Message}");
            return;
        }

        if (!cache.Probes.TryGetValue(methodToken, out var probes) || probes.Count == 0)
        {
            Logger.Error($"No probes found for method with token '{methodToken}'");
            return;
        }

        if (!cache.SourceFile.TryGetValue(methodToken, out var pdbSourceFile))
        {
            Logger.Error($"No source code for found for method with token '{methodToken}'");
            return;
        }

        if (!cache.Metadata.TryGetValue(methodToken, out var metadata))
        {
            Logger.Error($"No metadata found for method with token '{methodToken}'");
            return;
        }

        // Add span tags
        var joinedSourceLines = string.Join(Environment.NewLine, pdbSourceFile.Lines);

        activity.AddTag("source.text", joinedSourceLines);
        activity.AddTag("source.name", pdbSourceFile.Name);
        activity.AddTag("method.name", metadata.Name);
        activity.AddTag("method.type_name", metadata.TypeName);
        activity.AddTag("method.start_line", metadata.StartLine);
        activity.AddTag("method.end_line", metadata.EndLine);

        var branches = GetCoveredBranches(probes, pProbes, probeLength);
        var executedLines = branches.SelectMany(b => b.SourceLines);
        var formattedLine = string.Join(',', executedLines);
        activity.AddTag("method.lines_executed", formattedLine);

        foreach (var branch in branches)
        {
            branch.SourceName = pdbSourceFile.Name;
            AddVisitedBranchMetadataToActivity(activity, branch);
        }
    }

    private static unsafe List<Branch> GetCoveredBranches(
        Dictionary<int, ProbeCacheEntry> probesByOffset,
        bool* visitedProbes,
        int length)
    {
        var branches = new List<Branch>();
        var probes = probesByOffset.Values.ToArray();

        for (var i = 0; i < length; i++)
        {
            var probeWasVisited = visitedProbes[i];
            if (!probeWasVisited) continue;
            var probe = probes[i];
            var branch = new Branch(string.Empty, probe.CoveredSourceLines, probe.BlockId, probe.SourceLinesInBlock);
            branches.Add(branch);
        }

        return branches;
    }

    private static void AddVisitedBranchMetadataToActivity(
        Activity activity,
        Branch branch)
    {
        var formattedBlockLines = string.Join(',', branch.SourceLinesInBlock);

        var branchCounter = Telemetry.BranchCounter;

        branchCounter.Add(1,
            new KeyValuePair<string, object?>("branch.id", branch.BlockId),
            new KeyValuePair<string, object?>("branch.lines", formattedBlockLines)
        );

        activity.AddEvent(new ActivityEvent("coverage.branch", DateTimeOffset.UtcNow,
            new ActivityTagsCollection
            {
                { "branch.id", branch.BlockId },
                { "branch.lines", formattedBlockLines }
            }));
    }
}