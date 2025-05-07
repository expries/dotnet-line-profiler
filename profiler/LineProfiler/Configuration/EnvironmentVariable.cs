namespace LineProfiler.Configuration;

public class EnvironmentVariable(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}