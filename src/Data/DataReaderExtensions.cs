using Kros.KORM.Materializer;
using Kros.KORM.Query.Providers;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Asynchronously advances the <paramref name="reader"/> to the next record. Reader is read asynchronously
        /// if it supports it (<see cref="DbDataReader"/>, <see cref="IDataReaderEnvelope"/>,
        /// <see cref="ModelBuilder.QueryDataReader"/>), otherwise synchronous <see cref="IDataReader.Read"/> is used.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// Task with result <see langword="true"/> if there are more rows, otherwise <see langword="false"/>.
        /// </returns>
        internal static Task<bool> ReadAsync(this IDataReader reader, CancellationToken cancellationToken)
            => reader switch
            {
                DbDataReader dbReader => dbReader.ReadAsync(cancellationToken),
                IDataReaderEnvelope envelope => envelope.ReadAsync(cancellationToken),
                ModelBuilder.QueryDataReader queryReader => queryReader.ReadAsync(cancellationToken),
                _ => cancellationToken.IsCancellationRequested
                    ? Task.FromCanceled<bool>(cancellationToken)
                    : Task.FromResult(reader.Read())
            };
    }
}
