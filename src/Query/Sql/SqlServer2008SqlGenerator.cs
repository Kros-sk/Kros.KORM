using Kros.KORM.Metadata;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// SQL query generator for SQL Server 2008 and newer.
    /// </summary>
    /// <remarks>
    /// Offset (<c>Skip</c>) and limit (<c>Top</c>) are translated to CTE (Common Table Expression).
    /// </remarks>
    public class SqlServer2008SqlGenerator : DefaultQuerySqlGenerator
    {
        private const string CteQueryOffset =
            "WITH Results_CTE AS ({0}) SELECT * FROM Results_CTE WHERE __RowNum__ > {1}";
        private const string CteQueryLimitOffset =
            "WITH Results_CTE AS ({0}) SELECT * FROM Results_CTE WHERE __RowNum__ > {1} AND __RowNum__ <= {2}";

        /// <summary>
        /// Creates an instance of the generator with specified database mapper <paramref name="databaseMapper"/>.
        /// </summary>
        /// <param name="databaseMapper">Database mapper</param>
        public SqlServer2008SqlGenerator(IDatabaseMapper databaseMapper) : base(databaseMapper)
        {
        }

        /// <inheritdoc/>
        protected override void AddOrderBy()
        {
            if (Skip == 0)
            {
                base.AddOrderBy();
            }
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
                if (Orders.Count > 0)
                {
                    SqlBuilder.Insert(ColumnsPosition, $", ROW_NUMBER() OVER({CreateOrderByString()}) AS __RowNum__");
                }
                string baseSql = SqlBuilder.ToString();
                SqlBuilder.Clear();
                if (Top > 0)
                {
                    SqlBuilder.AppendFormat(CteQueryLimitOffset, baseSql, Skip, Skip + Top);
                }
                else
                {
                    SqlBuilder.AppendFormat(CteQueryOffset, baseSql, Skip);
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
