using Profiler.Core.Interfaces;

namespace Profiler.Core;

public abstract class Unknown : IUnknown
{
    private int _referenceCount;

    HResult IUnknown.QueryInterface(in Guid guid, out nint ptr)
    {
        return QueryInterface(guid, out ptr);
    }

    int IUnknown.AddRef()
    {
        return Interlocked.Increment(ref _referenceCount);
    }

    int IUnknown.Release()
    {
        var value = Interlocked.Decrement(ref _referenceCount);

        if (value == 0) Dispose();

        return value;
    }

    protected abstract HResult QueryInterface(in Guid guid, out nint ptr);

    public virtual void Dispose()
    {
    }
}