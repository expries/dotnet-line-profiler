using System.Runtime.InteropServices;

namespace SampleConsoleApp;

public static class ByteArrayHelper
{
    public static void SetProbes()
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

    public static unsafe void ProcessProbe(bool* data, int size)
    {
        Console.WriteLine("000 Received data with size: " + size);
        Console.WriteLine("chunk 0:");
        Console.WriteLine(data[0]);
        Console.WriteLine("chunk 1:");
        Console.WriteLine(data[1]);
    }
}