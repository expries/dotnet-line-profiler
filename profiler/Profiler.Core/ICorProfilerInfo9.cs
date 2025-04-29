using NativeObjects;

namespace Profiler.Core;

public class ICorProfilerInfo9 : ICorProfilerInfo8
{
    private ICorProfilerInfo9Invoker _impl;

    public ICorProfilerInfo9(IntPtr ptr) : base(ptr)
    {
        _impl = new ICorProfilerInfo9Invoker(ptr);
    }

    public unsafe HResult GetNativeCodeStartAddresses(FunctionId functionID, ReJITId reJitId, uint cCodeStartAddresses,
        uint* pcCodeStartAddresses, uint* codeStartAddresses)
    {
        return _impl.GetNativeCodeStartAddresses(functionID, reJitId, cCodeStartAddresses, pcCodeStartAddresses,
            codeStartAddresses);
    }

    public unsafe HResult GetILToNativeMapping3(uint* pNativeCodeStartAddress, uint cMap, uint* pcMap,
        COR_DEBUG_IL_TO_NATIVE_MAP* map)
    {
        return _impl.GetILToNativeMapping3(pNativeCodeStartAddress, cMap, pcMap, map);
    }

    public unsafe HResult GetCodeInfo4(uint* pNativeCodeStartAddress, uint cCodeInfos, uint* pcCodeInfos,
        COR_PRF_CODE_INFO* codeInfos)
    {
        return _impl.GetCodeInfo4(pNativeCodeStartAddress, cCodeInfos, pcCodeInfos, codeInfos);
    }
}