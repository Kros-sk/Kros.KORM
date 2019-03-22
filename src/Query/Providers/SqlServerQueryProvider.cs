using Kros.Data.BulkActions;
using Kros.Data.BulkActions.SqlServer;
using Kros.Data.Schema;
using Kros.Data.Schema.SqlServer;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Query.Sql;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Provider, which know execute query for Sql Server.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.QueryProvider" />
    public class SqlServerQueryProvider : QueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerQueryProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string settings.</param>
        /// <param name="sqlGeneratorFactory">The SQL generator factory.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="logger">The logger.</param>
        public SqlServerQueryProvider(
            ConnectionStringSettings connectionString,
            ISqlExpressionVisitorFactory sqlGeneratorFactory,
            IModelBuilder modelBuilder,
            ILogger logger)
            : base(connectionString, sqlGeneratorFactory, modelBuilder, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryProvider" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="sqlGeneratorFactory">The SQL generator factory.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="logger">The logger.</param>
        public SqlServerQueryProvider(
            DbConnection connection,
            ISqlExpressionVisitorFactory sqlGeneratorFactory,
            IModelBuilder modelBuilder,
            ILogger logger)
            : base(connection, sqlGeneratorFactory, modelBuilder, logger)
        {
        }

        /// <summary>
        /// Returns <see cref="DbProviderFactory"/> for current provider.
        /// </summary>
        public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

        /// <summary>
        /// Creates instance of <see cref="IBulkInsert" />.
        /// </summary>
        /// <returns>
        /// Instance of <see cref="IBulkInsert" />.
        /// </returns>
        public override IBulkInsert CreateBulkInsert()
        {
            var transaction = GetCurrentTransaction();
            if (IsExternalConnection || transaction != null)
            {
                return new SqlServerBulkInsert(Connection as SqlConnection, transaction as SqlTransaction);
            }
            else
            {
                return new SqlServerBulkInsert(ConnectionString);
            }
        }

        /// <summary>
        /// Creates instance of <see cref="IBulkUpdate" />.
        /// </summary>
        /// <returns>
        /// Instance of <see cref="IBulkUpdate" />.
        /// </returns>
        public override IBulkUpdate CreateBulkUpdate()
        {
            var transaction = GetCurrentTransaction();

            if (IsExternalConnection || transaction != null)
            {
                return new SqlServerBulkUpdate(Connection as SqlConnection, transaction as SqlTransaction);
            }
            else
            {
                return new SqlServerBulkUpdate(ConnectionString);
            }
        }

        /// <summary>
        /// Returns instance of <see cref="SqlServerSchemaLoader"/>.
        /// </summary>
        protected override IDatabaseSchemaLoader GetSchemaLoader()
        {
            return new SqlServerSchemaLoader();
        }
    }
}
