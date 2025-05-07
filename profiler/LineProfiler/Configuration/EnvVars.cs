namespace LineProfiler.Configuration;

public static class EnvVars
{
    public static EnvironmentVariable PdbDirectory = new("PROFILER_PDB_DIRECTORY", string.Empty);
}
