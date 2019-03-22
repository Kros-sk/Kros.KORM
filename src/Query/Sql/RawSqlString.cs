using System;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// A string representing a raw SQL query. This type enables overload resolution between the regular and interpolated
    /// SQL string query.
    /// </summary>
    public struct RawSqlString
    {
        /// <summary>
        /// Implicitly converts a <see cref="string" /> to a <see cref="RawSqlString" />
        /// </summary>
        /// <param name="s"> The string. </param>
        public static implicit operator RawSqlString(string s) => new RawSqlString(s);

        /// <summary>
        /// Implicitly converts a <see cref="FormattableString" /> to a <see cref="RawSqlString" />
        /// </summary>
        /// <param name="fs"> The string format. </param>
        public static implicit operator RawSqlString(FormattableString fs) =>
            new RawSqlString(fs.Format.Replace("{", "@").Replace("}", string.Empty));

        /// <summary>
        /// Constructs a <see cref="RawSqlString" /> from a see <see cref="string" />
        /// </summary>
        /// <param name="s"> The string. </param>
        public RawSqlString(string s) => Format = s;

        /// <summary>
        /// The string format.
        /// </summary>
        public string Format { get; }
    }
}
