using Kros.Data.BulkActions;
using Kros.Data.BulkActions.SqlServer;
using Kros.Data.Schema;
using Kros.Data.Schema.SqlServer;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query.Sql;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Provider, which know execute query for Sql Server.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.QueryProvider" />
    public class SqlServerQueryProvider : QueryProvider
    {
        private readonly IAuthTokenProvider _tokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerQueryProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string settings.</param>
        /// <param name="sqlGeneratorFactory">The SQL generator factory.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="databaseMapper">The Database mapper.</param>
        /// <param name="tokenProvider">Provider to support token-based authentication.</param>
        public SqlServerQueryProvider(
            KormConnectionSettings connectionString,
            ISqlExpressionVisitorFactory sqlGeneratorFactory,
            IModelBuilder modelBuilder,
            ILogger logger,
            IDatabaseMapper databaseMapper,
            IAuthTokenProvider tokenProvider)
            : base(connectionString, sqlGeneratorFactory, modelBuilder, logger, databaseMapper)
        {
            _tokenProvider = tokenProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryProvider" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="sqlGeneratorFactory">The SQL generator factory.</param>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="databaseMapper">The Database mapper.</param>
        /// <param name="tokenProvider">Provider to support token-based authentication.</param>
        public SqlServerQueryProvider(
            DbConnection connection,
            ISqlExpressionVisitorFactory sqlGeneratorFactory,
            IModelBuilder modelBuilder,
            ILogger logger,
            IDatabaseMapper databaseMapper,
            IAuthTokenProvider tokenProvider)
            : base(connection, sqlGeneratorFactory, modelBuilder, logger, databaseMapper)
        {
            _tokenProvider = tokenProvider;
        }

        /// <summary>
        /// Returns <see cref="DbProviderFactory"/> for current provider.
        /// </summary>
        public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

        /// <summary>
        /// Returns (creates if needed) connection. If <see cref="IAuthTokenProvider"/> was setup in constructor,
        /// it is used to set the <see cref="SqlConnection.AccessToken">AccessToken</see> on connection.
        /// </summary>
        /// <returns><see cref="DbConnection"/> instance.</returns>
        protected override DbConnection GetConnection()
        {
            var connection = (SqlConnection)base.GetConnection();
            SetAccessToken(connection);
            return connection;
        }

        private void SetAccessToken(SqlConnection connection)
        {
            if (_tokenProvider != null)
            {
                connection.AccessToken = _tokenProvider.GetToken();
            }
        }

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
                return new SqlServerBulkInsert(GetConnection() as SqlConnection, transaction as SqlTransaction);
            }
            else
            {
                return new SqlServerBulkInsert(CreateConnection());
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
                return new SqlServerBulkUpdate(GetConnection() as SqlConnection, transaction as SqlTransaction);
            }
            else
            {
                return new SqlServerBulkUpdate(CreateConnection());
            }
        }

        private SqlConnection CreateConnection()
        {
            var connection = (SqlConnection)DbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            SetAccessToken(connection);
            return connection;
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
