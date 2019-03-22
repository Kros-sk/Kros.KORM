using Kros.KORM.Metadata;
using Kros.KORM.Query.Providers;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// SQL query generator for SQL Server 2012 and newer.
    /// </summary>
    /// <remarks>
    /// Offset (<c>Skip</c>) and limit (<c>Top</c>) are translated to SQL server 2012 syntax:
    /// <c>OFFSET n ROWS FETCH NEXT m ROWS ONLY</c>.
    /// </remarks>
    public class SqlServer2012SqlGenerator : DefaultQuerySqlGenerator
    {
        /// <summary>
        /// Creates an instance of the generator with specified database mapper <paramref name="databaseMapper"/>.
        /// </summary>
        /// <param name="databaseMapper">Database mapper</param>
        public SqlServer2012SqlGenerator(IDatabaseMapper databaseMapper) : base(databaseMapper)
        {
        }

        /// <inheritdoc/>
        protected override void AddLimitAndOffset()
        {
            if (Skip == 0)
            {
                base.AddLimitAndOffset();
            }
            else
            {
                SqlBuilder.AppendFormat(" OFFSET {0} ROWS", Skip);
                if (Top > 0)
                {
                    SqlBuilder.AppendFormat(" FETCH NEXT {0} ROWS ONLY", Top);
                }
            }
        }

        /// <summary>
        /// Returns <see langword="null"/>.
        /// </summary>
        /// <returns>Returns <see langword="null"/>.</returns>
        protected override IDataReaderEnvelope CreateQueryReader() => null;
    }
}
