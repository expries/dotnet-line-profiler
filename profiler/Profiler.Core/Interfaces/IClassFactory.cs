namespace Profiler.Core.Interfaces;

[NativeObject]
public interface IClassFactory : IUnknown
{
    HResult CreateInstance(IntPtr outer, in Guid guid, out IntPtr instance);

    HResult LockServer(bool @lock);

}