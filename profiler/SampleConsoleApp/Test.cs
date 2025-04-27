using System.Diagnostics;
namespace SampleConsoleApp;

public class Test
{
    public static void CallTrace()
    {
        Console.WriteLine("Test");
        var activity = Activity.Current;
        Console.WriteLine("Test");
        var traceId = activity?.TraceId.ToHexString();
        Console.WriteLine("Test");
        UseTrace(traceId);
    }

    public static void UseTrace(string? traceId)
    {
        Console.WriteLine($"Trace id: {traceId}");
        if (traceId is not null)
        {
            var myBool = traceId.StartsWith("test");
            Console.WriteLine(myBool ? 'y' : 'n');
        } 
    }
    
    public static void For(int limit)
    {
        Console.WriteLine($"For: limit={limit}");
        
        for (var i = 0; i < limit; i++)
        {
            Console.WriteLine($"i={i + 1}/{limit}");
        }

        var x = limit + 12;
        Console.WriteLine("LOL1");
        Console.WriteLine("LOL2: " + x);
        Console.WriteLine("LOL3");
    }

    public static void Linear()
    {
        Console.WriteLine("Linear");
    }
    
    public static void If(int x)
    {
        Console.WriteLine($"If: x={x}");
        
        if (x > 0)
        {
            Console.WriteLine("x > 0");
        }
        
        Console.WriteLine("Return");
    }
    
    public static void IfElse(int x)
    {
        Console.WriteLine($"IfElse: x={x}");
        
        if (x > 0)
        {
            Console.WriteLine("x > 0");
        }
        else
        {
            Console.WriteLine("x <= 0");
        }
        
        Console.WriteLine("Return");
    }
    
    public static void IfElseIfElse(int x)
    {      
        Console.WriteLine($"IfElseIfElse: x={x}");
        
        if (x > 0)
        {
            Console.WriteLine("x > 0");
        }
        else if (x > -10)
        {
            Console.WriteLine("x > -10");
        }
        else
        {
            Console.WriteLine("x <= 0");
        }
        
        Console.WriteLine("Return");
    }
    
    public static void Switch(int x, int y)
    {
        Console.WriteLine($"Switch: x={x}");
        
        switch (x)
        {
            case 0:
                if (y > 0)
                {
                    Console.WriteLine("y > 0");
                    Console.WriteLine("y > 0");
                }
                else
                {
                    Console.WriteLine("y <= 0");
                    Console.WriteLine("y <= 0");
                    Console.WriteLine("y <= 0");
                }
                Console.WriteLine("x == 0");
                break;
            case 1:
                Console.WriteLine("x == 1");
                break;
            case 2:
                Console.WriteLine("x == 2");
                break;
        }

        Console.WriteLine("Return");
    }
}