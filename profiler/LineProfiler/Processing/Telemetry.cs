using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LineProfiler.Processing;

public class Telemetry
{
    private const string ACTIVITY_SOURCE_NAME = nameof(Telemetry);
    private static readonly Meter BranchMeter = new("LineProfiler.Branch");
    
    public static readonly ActivitySource ActivitySource = new(ACTIVITY_SOURCE_NAME);
    
    public static readonly Counter<long> BranchCounter = BranchMeter.CreateCounter<long>(
        "coverage.branch_executions", "count", "branch executions");
}