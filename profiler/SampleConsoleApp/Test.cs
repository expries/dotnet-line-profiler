namespace SampleConsoleApp;

public class Test
{
    private readonly int _x;

    public Test(int x)
    {
        _x = x;
    }

    public void OneLocal()
    {
        var size = new Random(400).Next(0, 200);

        if (size is > 50)
        {
            return;
        }
        
        switch (size)
        {
            case < 100:
                Console.WriteLine("NOT OK");
                return;
            case > 100:
                Console.WriteLine("OK");
                return;
        }
    }
    
    public void TwoLocals()
    {
        Console.WriteLine("Hello world!");
    }
    
    public void ThreeLocals()
    {
        var x = 0;
        var y = 1;
        var z = 2;
        Console.WriteLine(x + y + z);
    }



    public void If(int x)
    {
        Console.WriteLine($"If: x={x}");
        
        if (x > 0)
        {
            Console.WriteLine("x > 0");
        }
        
        Console.WriteLine("Return");
    }
    
    public void IfElse(int x)
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
    
    public void IfElseIfElse(int x)
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
    
    public void Switch(int x, int y)
    {
        Console.WriteLine($"Switch: x={x}");
        
        switch (x)
        {
            case 0:
                if (y > 0)
                {
                    Console.WriteLine("x > 0");
                }
                else
                {
                    Console.WriteLine("x <= 0");
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
    
    public void X()
    {
        unsafe
        {
            fixed (bool* probes = new bool[255])
            {
                probes[110] = true;
                probes[120] = true;
                probes[130] = true;
                ProcessProbe(probes, 255);
            }
        }
        
    }

    public static unsafe void ProcessProbe(bool* data, uint size)
    {
    }
}