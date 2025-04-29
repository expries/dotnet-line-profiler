using NativeObjects;

namespace Profiler.Core;

public class ICorProfilerInfo5 : ICorProfilerInfo4
{
    private ICorProfilerInfo5Invoker _impl;

    public ICorProfilerInfo5(IntPtr ptr) : base(ptr)
    {
        _impl = new ICorProfilerInfo5Invoker(ptr);
    }

    public HResult GetEventMask2(out CorPrfMonitor pdwEventsLow, out CorPrfHighMonitor pdwEventsHigh)
    {
        return _impl.GetEventMask2(out pdwEventsLow, out pdwEventsHigh);
    }

    public HResult SetEventMask2(CorPrfMonitor dwEventsLow, CorPrfHighMonitor dwEventsHigh)
    {
        return _impl.SetEventMask2(dwEventsLow, dwEventsHigh);
    }
}