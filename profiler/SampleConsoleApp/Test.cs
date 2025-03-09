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

        if (size is > 50 and < 120)
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



    public void Y()
    {
        var x = 1;
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