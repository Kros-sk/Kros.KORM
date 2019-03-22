using Kros.Data.SqlServer;
using Kros.KORM.Metadata;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Kros.KORM.Query.Providers
{
    /// <summary>
    /// <inheritdoc cref="ISqlExpressionVisitorFactory"/>
    /// </summary>
    public class SqlServerSqlExpressionVisitorFactory : ISqlExpressionVisitorFactory
    {
        private readonly IDatabaseMapper _databaseMapper;

        /// <summary>
        /// Creates an instance with specified database mapper <paramref name="databaseMapper"/>.
        /// </summary>
        /// <param name="databaseMapper">Database mapper.</param>
        public SqlServerSqlExpressionVisitorFactory(IDatabaseMapper databaseMapper)
        {
            _databaseMapper = Check.NotNull(databaseMapper, nameof(databaseMapper));
        }

        /// <summary>
        /// Creates an <see cref="ISqlExpressionVisitor"/> based on version of SQL server <paramref name="connection"/>.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="SqlServer2012SqlGenerator"/> if SQL server version is 2012 or greater.</item>
        /// <item><see cref="SqlServer2008SqlGenerator"/> if SQL server version is 2008 or greater.</item>
        /// <item><see cref="DefaultQuerySqlGenerator"/> for older SQL server.</item>
        /// </list>
        /// </returns>
        public ISqlExpressionVisitor CreateVisitor(IDbConnection connection)
        {
            Version sqlServerVersion = (connection as SqlConnection).GetVersion();
            if (sqlServerVersion >= SqlServerVersions.Server2012)
            {
                return new SqlServer2012SqlGenerator(_databaseMapper);
            }
            else if (sqlServerVersion >= SqlServerVersions.Server2008)
            {
                return new SqlServer2008SqlGenerator(_databaseMapper);
            }
            return new DefaultQuerySqlGenerator(_databaseMapper);
        }
    }
}
