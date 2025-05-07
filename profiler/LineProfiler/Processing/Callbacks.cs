using IlRewriter.Models.Signatures;
using Profiler.Core;

namespace LineProfiler.Processing;

public static class Callbacks
{
    public static readonly MethodSignature Signature = new(
        CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
        CorElementTypeFlags.ELEMENT_TYPE_VOID,
        // Number of arguments
        5,
        // Method token
        CorElementTypeFlags.ELEMENT_TYPE_I4,
        // Bool array pointer (pinned)
        CorElementTypeFlags.ELEMENT_TYPE_PTR, CorElementTypeFlags.ELEMENT_TYPE_BOOLEAN,
        // Bool array length
        CorElementTypeFlags.ELEMENT_TYPE_I4,
        // Cache data IntPtr (pinned)
        CorElementTypeFlags.ELEMENT_TYPE_I,
        // Cache data length IntPtr (pinned)
        CorElementTypeFlags.ELEMENT_TYPE_I);
}