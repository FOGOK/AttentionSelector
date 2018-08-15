using System;

namespace AttentionSelector
{
    public static class DateTimeExtensions
    {
        public static long ToUTC(this DateTime dateTime)
        {
            return (long) (dateTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}