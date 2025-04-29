using System.Diagnostics;

namespace SampleAspNetApp;

public class CustomerService
{
    public static int GetNumberOfCustomers()
    {
        Console.WriteLine("Loading customers ...");

        if (Activity.Current is not null)
        {
            var traceId = Activity.Current.Id;
            Console.WriteLine("Trace id: " + traceId);
        }
        else
        {
            Console.WriteLine("No active Activity");
        }

        return Random.Shared.Next(1, 100);
    }
}