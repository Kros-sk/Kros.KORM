using Kros.Data.SqlServer;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query.Providers;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Factory which create sql server provider.
    /// </summary>
    public class SqlServerQueryProviderFactory : IQueryProviderFactory
    {
        /// <summary>
        /// Creates the SqlServer query provider.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="databaseMapper">Database mapper.</param>
        /// <returns>
        /// Instance of <see cref="SqlServerQueryProvider"/>.
        /// </returns>
        public IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
            => new SqlServerQueryProvider(
                connection, new SqlServerSqlExpressionVisitorFactory(databaseMapper), modelBuilder, new Logger());

        /// <summary>
        /// Creates the SqlServer query provider.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="databaseMapper">Database mapper.</param>
        /// <returns>
        /// Instance of <see cref="SqlServerQueryProvider"/>.
        /// </returns>
        public IQueryProvider Create(
            ConnectionStringSettings connectionString,
            IModelBuilder modelBuilder,
            IDatabaseMapper databaseMapper)
            => new SqlServerQueryProvider(
                connectionString, new SqlServerSqlExpressionVisitorFactory(databaseMapper), modelBuilder, new Logger());

        /// <summary>
        /// Registers instance of this type to <see cref="QueryProviderFactories"/>.
        /// </summary>
        internal static void Register()
        {
            QueryProviderFactories.Register<SqlConnection>(SqlServerDataHelper.ClientId, new SqlServerQueryProviderFactory());
        }
    }
}
