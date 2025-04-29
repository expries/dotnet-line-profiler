using System.Runtime.InteropServices;
using Profiler.Core;

namespace LineProfiler;

public class DllMain
{
    private static ClassFactory? _instance;

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject")]
    public static unsafe int DllGetClassObject(Guid* rclsid, Guid* riid, IntPtr* ppv)
    {
        _instance = new ClassFactory(new CorProfiler());
        *ppv = _instance.IClassFactory;
        return 0;
    }
}