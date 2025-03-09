using System.Diagnostics;

namespace SampleAspNetApp;

public class CustomerService
{
    public static int GetNumberOfCustomers()
    {
        Console.WriteLine("Loading customers ...");
        var traceId = Activity.Current?.Id;
        Console.WriteLine("Trace id: " + traceId);
        return Random.Shared.Next(1, 100);
    }
    
}