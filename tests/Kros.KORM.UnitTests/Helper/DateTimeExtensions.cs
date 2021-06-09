using System;
using System.Globalization;

namespace Kros.KORM.UnitTests.Helper
{
    internal static class DateTimeExtensions
    {
        public static DateTime ParseDateTime(this string dateTime)
            => DateTime.Parse(dateTime, CultureInfo.InvariantCulture);
    }
}
