using System.Runtime.InteropServices;

namespace LineProfiler.Helpers;

public static class UnsafeTypes
{
    public static unsafe byte[] GetByteArrayFromPtr(IntPtr* ptr, uint length)
    {
        var byteArray = new byte[length];
        Marshal.Copy((IntPtr)ptr, byteArray, 0, (int)length);
        return byteArray;
    }

    public static unsafe byte* GetBytePtr(byte[] data)
    {
        fixed (byte* pointer = data)
        {
            for (var i = 0; i < data.Length; i++) pointer[i] = data[i];
            return pointer;
        }
    }

    public static unsafe string GetStringFromCharPtr(char* pString, uint size)
    {
        var str = string.Empty;

        for (var i = 0; i < size; i++) str += pString[i];

        return str;
    }

    public static unsafe MemoryStream GetStreamFromBytePtr(byte* pData, uint size)
    {
        var data = new byte[size];
        Marshal.Copy((IntPtr)pData, data, 0, (int)size);
        return new MemoryStream(data);
    }

    public static unsafe byte[] GetByteArrayFromBytePtr(byte* pData, uint size)
    {
        var array = new byte[size];
        for (var i = 0; i < size; i++) array[i] = pData[i];
        return array;
    }

    public static string ConvertSpanToString(Span<char> span, uint length)
    {
        var lengthAsInt32 = Convert.ToInt32(length);
        return span[..lengthAsInt32].ToString().Trim();
    }

    public static Span<char> CreateSpanForName()
    {
        return new Span<char>(new char[1024]);
    }
}