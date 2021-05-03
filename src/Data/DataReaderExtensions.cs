using System;
using System.Data;

namespace Kros.KORM.Data
{
    internal static class DataReaderExtensions
    {
        public static bool? GetNullableBoolean(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetBoolean(i);

        public static byte? GetNullableByte(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetByte(i);

        public static char? GetNullableChar(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetChar(i);

        public static DateTime? GetNullableDateTime(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetDateTime(i);

        public static decimal? GetNullableDecimal(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetDecimal(i);

        public static double? GetNullableDouble(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetDouble(i);

        public static Guid? GetNullableGuid(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetGuid(i);

        public static short? GetNullableInt16(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetInt16(i);

        public static int? GetNullableInt32(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetInt32(i);

        public static long? GetNullableInt64(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetInt64(i);

        public static float? GetNullableFloat(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? null : reader.GetFloat(i);
    }
}
