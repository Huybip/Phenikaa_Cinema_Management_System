using System;
using System.Globalization;

namespace PhenikaaCinema.Helpers
{
    public static class CurrencyHelper
    {
        private static readonly CultureInfo VietnamCulture = new CultureInfo("vi-VN");

        public static string FormatVnd(decimal amount)
        {
            return string.Format(VietnamCulture, "{0:N0} ₫", amount);
        }

        public static string FormatVnd(decimal? amount)
        {
            if (!amount.HasValue) return "0 ₫";
            return FormatVnd(amount.Value);
        }

        public static string ToVnd(this decimal amount)
        {
            return FormatVnd(amount);
        }

        public static string ToVnd(this decimal? amount)
        {
            return FormatVnd(amount);
        }
    }
}
