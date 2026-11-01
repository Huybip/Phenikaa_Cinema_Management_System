using System;

namespace PhenikaaCinema.Helpers
{
    public static class DateTimeHelper
    {
        // Múi giờ Việt Nam (UTC + 7)
        public static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.ToUniversalTime(), VietnamTimeZone);
        }

        public static DateTime ToUtcTime(DateTime vietnamDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }

        public static string FormatVietnamDateTime(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "-";
            DateTime vnTime = ToVietnamTime(dateTime.Value);
            return vnTime.ToString("dd/MM/yyyy HH:mm");
        }

        public static string FormatVietnamTime(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "-";
            DateTime vnTime = ToVietnamTime(dateTime.Value);
            return vnTime.ToString("HH:mm");
        }

        public static string FormatVietnamDate(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "-";
            DateTime vnTime = ToVietnamTime(dateTime.Value);
            return vnTime.ToString("dd/MM/yyyy");
        }
    }
}
