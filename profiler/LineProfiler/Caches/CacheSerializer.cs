using System.Text;
using LineProfiler.DebugInfo;

namespace LineProfiler.Caches;

public static class CacheSerializer
{
    /// <summary>
    /// Simple serialization to a byte array using BinaryWriter.
    /// </summary>
    public static byte[] Serialize(CallbackCache cache)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        // Serialize Probes
        writer.Write(cache.Probes.Count);
        foreach (var outer in cache.Probes)
        {
            writer.Write(outer.Key);
            var inner = outer.Value;
            writer.Write(inner.Count);
            foreach (var kv in inner)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value.BlockId);

                writer.Write(kv.Value.CoveredSourceLines.Length);
                foreach (var off in kv.Value.CoveredSourceLines)
                    writer.Write(off);

                writer.Write(kv.Value.SourceLinesInBlock.Length);
                foreach (var off in kv.Value.SourceLinesInBlock)
                    writer.Write(off);
            }
        }

        // Serialize SourceFile
        writer.Write(cache.SourceFile.Count);
        foreach (var kv in cache.SourceFile)
        {
            writer.Write(kv.Key);
            writer.Write(kv.Value.Name);
            writer.Write(kv.Value.Lines.Length);
            foreach (var line in kv.Value.Lines)
                writer.Write(line);
        }

        // Serialize Metadata
        writer.Write(cache.Metadata.Count);
        foreach (var kv in cache.Metadata)
        {
            writer.Write(kv.Key);
            writer.Write(kv.Value.Name);
            writer.Write(kv.Value.TypeName);
            writer.Write(kv.Value.StartLine);
            writer.Write(kv.Value.EndLine);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Simple deserialization from a byte array using BinaryReader.
    /// </summary>
    public static CallbackCache Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        // Deserialize Probes
        int outerCount = reader.ReadInt32();
        var probes = new Dictionary<int, Dictionary<int, ProbeCacheEntry>>(outerCount);
        for (int i = 0; i < outerCount; i++)
        {
            int outerKey = reader.ReadInt32();
            int innerCount = reader.ReadInt32();
            var inner = new Dictionary<int, ProbeCacheEntry>(innerCount);
            for (int j = 0; j < innerCount; j++)
            {
                int innerKey = reader.ReadInt32();
                string blockId = reader.ReadString();

                int covCount = reader.ReadInt32();
                var cov = new int[covCount];
                for (int k = 0; k < covCount; k++) cov[k] = reader.ReadInt32();

                int blkCount = reader.ReadInt32();
                var blk = new int[blkCount];
                for (int k = 0; k < blkCount; k++) blk[k] = reader.ReadInt32();

                inner[innerKey] = new ProbeCacheEntry(blockId, cov, blk);
            }

            probes[outerKey] = inner;
        }

        // Deserialize SourceFile
        int srcCount = reader.ReadInt32();
        var sourceFile = new Dictionary<int, PdbSourceCodeFile>(srcCount);
        for (int i = 0; i < srcCount; i++)
        {
            int key = reader.ReadInt32();
            string name = reader.ReadString();
            int lineCount = reader.ReadInt32();
            var lines = new string[lineCount];
            for (int j = 0; j < lineCount; j++) lines[j] = reader.ReadString();
            sourceFile[key] = new PdbSourceCodeFile(name, lines);
        }

        // Deserialize Metadata
        int mdCount = reader.ReadInt32();
        var metadata = new Dictionary<int, MethodMetadata>(mdCount);
        for (int i = 0; i < mdCount; i++)
        {
            int key = reader.ReadInt32();
            string name = reader.ReadString();
            string typeName = reader.ReadString();
            int startLine = reader.ReadInt32();
            int endLine = reader.ReadInt32();
            metadata[key] = new MethodMetadata(name, typeName, startLine, endLine);
        }

        return new CallbackCache(probes, sourceFile, metadata);
    }
}