using System.Diagnostics;

namespace SampleAspNetApp;

public class CustomerService
{
    public static int GetNumberOfCustomers()
    {
        Console.WriteLine("Loading customers ...");
        var activityIsSet = Activity.Current is not null;

        if (activityIsSet)
        {
            var traceId = Activity.Current!.TraceId.ToHexString();
            Console.WriteLine("Trace id: " + traceId);
        }
        else
        {
            Console.WriteLine("Activity set: " + bool.FalseString);
        }

        return Random.Shared.Next(1, 100);
    }
}