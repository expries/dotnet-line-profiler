namespace ProcessingAPI.Web;

public static class DateTimeExtensions
{
    public static DateTime FromNanoseconds(ulong nanoseconds)
    {
        var epoch = DateTime.UnixEpoch;
        var nanosecondsLong = Convert.ToInt64(nanoseconds);
        var result = epoch.AddTicks(nanosecondsLong / 100);
        return result;
    }
}