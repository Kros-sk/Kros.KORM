using Kros.Data.BulkActions;
using Kros.KORM.Data;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Kros.KORM
{
    /// <summary>
    /// Represent access to ORM features.
    /// <para>
    /// For executing query and materializing models see:
    /// <para >
    /// <see cref="Kros.KORM.IDatabase" />
    /// </para>
    /// <para>
    /// <see cref="Kros.KORM.Query.IQuery{T}" />
    /// </para>
    /// </para>
    /// </summary>
    /// <seealso cref="Kros.KORM.Materializer.IModelBuilder" />
    public class Database : IDatabase
    {
        #region Static

        /// <summary>
        /// Gets or sets the default model mapper, which will be used for mapping Object to Relation database.
        /// </summary>
        /// <value>
        /// The default model mapper.
        /// </value>
        public static IModelMapper DefaultModelMapper { get; set; }

        /// <summary>
        /// Gets or sets the default model factory, which will be used for instanting and filling object from Ado.
        /// </summary>
        /// <value>
        /// The default model factory.
        /// </value>
        public static IModelFactory DefaultModelFactory { get; set; }

        /// <summary>
        /// Gets the database mapper, which has mapping information for all tables in database
        /// </summary>
        public static IDatabaseMapper DatabaseMapper { get; private set; }

        /// <summary>
        /// Gets or sets the logging delegate.
        /// </summary>
        public static Action<string> Log { get; set; }

        #endregion

        #region Private fields

        private IModelBuilder _modelBuilder;
        private IQueryProvider _queryProvider;
        private IDatabaseMapper _databaseMapper;

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor for initializing default behaviours.
        /// </summary>
        static Database()
        {
            SqlServerQueryProviderFactory.Register();

            DefaultModelMapper = new ConventionModelMapper();
            DatabaseMapper = new DatabaseMapper(DefaultModelMapper);
            DefaultModelFactory = new DynamicMethodModelFactory(DatabaseMapper);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">The active connection.</param>
        public Database(ConnectionStringSettings connectionString)
            : this(connectionString, QueryProviderFactories.GetFactory(connectionString.ProviderName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database" /> class.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="adoClientName">Ado client name. (System.Data.SqlClient/System.Data.OleDb)</param>
        public Database(string connectionString, string adoClientName)
            : this(new ConnectionStringSettings("KORM", connectionString, adoClientName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string settings.</param>
        /// <param name="queryProviderFactory">The query provider factory, which know create query provider.</param>
        public Database(ConnectionStringSettings connectionString, IQueryProviderFactory queryProviderFactory)
        {
            Check.NotNull(connectionString, nameof(connectionString));
            Check.NotNull(queryProviderFactory, nameof(queryProviderFactory));

            _databaseMapper = DatabaseMapper;
            _modelBuilder = new ModelBuilder(Database.DefaultModelFactory);
            _queryProvider = queryProviderFactory.Create(connectionString, _modelBuilder, Database.DatabaseMapper);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="activeConnection">The active connection.</param>
        public Database(DbConnection activeConnection)
            : this(activeConnection, QueryProviderFactories.GetFactory(activeConnection))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database" /> class.
        /// </summary>
        /// <param name="activeConnection">The active connection.</param>
        /// <param name="queryProviderFactory">The query provider factory, which know create query provider.</param>
        public Database(DbConnection activeConnection, IQueryProviderFactory queryProviderFactory)
        {
            Check.NotNull(activeConnection, nameof(activeConnection));
            Check.NotNull(queryProviderFactory, nameof(queryProviderFactory));

            Init(activeConnection, queryProviderFactory, Database.DatabaseMapper, Database.DefaultModelFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database" /> class.
        /// </summary>
        /// <param name="activeConnection">The active connection.</param>
        /// <param name="modelMapper">Model mapper, which will be used for mapping Object to Relation database.</param>
        public Database(DbConnection activeConnection, IModelMapper modelMapper)
        {
            Check.NotNull(activeConnection, nameof(activeConnection));
            Check.NotNull(modelMapper, nameof(modelMapper));

            var databaseMapper = new DatabaseMapper(modelMapper);
            var defaultModelFactory = new DynamicMethodModelFactory(databaseMapper);

            Init(activeConnection, QueryProviderFactories.GetFactory(activeConnection),
                databaseMapper, defaultModelFactory);
        }

        private void Init(
            DbConnection activeConnection,
            IQueryProviderFactory queryProviderFactory,
            IDatabaseMapper databaseMapper,
            IModelFactory defaultModelFactory)
        {
            _databaseMapper = databaseMapper;
            _modelBuilder = new ModelBuilder(defaultModelFactory);
            _queryProvider = queryProviderFactory.Create(activeConnection, _modelBuilder, databaseMapper);
        }

        #endregion

        #region Database

        /// <summary>
        /// Returns <see cref="DbProviderFactory"/> for current provider.
        /// </summary>
        public DbProviderFactory DbProviderFactory => _queryProvider.DbProviderFactory;

        /// <summary>
        /// Creates instance of <see cref="IBulkInsert"/>.
        /// </summary>
        /// <returns>Instance of <see cref="IBulkInsert"/>.</returns>
        public IBulkInsert CreateBulkInsert() => _queryProvider.CreateBulkInsert();

        /// <summary>
        /// Creates instance of <see cref="IBulkUpdate"/>.
        /// </summary>
        /// <returns>Instance of <see cref="IBulkUpdate"/>.</returns>
        public IBulkUpdate CreateBulkUpdate() => _queryProvider.CreateBulkUpdate();

        /// <summary>
        /// Gets the model builder for materializing data from ado to models.
        /// </summary>
        public IModelBuilder ModelBuilder => _modelBuilder;

        /// <summary>
        /// Gets the query builder for T creating and executing query for obtains models.
        /// </summary>
        /// <typeparam name="T">Type of model, for which querying.</typeparam>
        public IQuery<T> Query<T>() => new Query<T>(_databaseMapper, _queryProvider);

        /// <inheritdoc/>
        public int ExecuteNonQuery(string query) => _queryProvider.ExecuteNonQuery(query);

        /// <inheritdoc/>
        public int ExecuteNonQuery(string query, CommandParameterCollection parameters)
            => _queryProvider.ExecuteNonQuery(query, parameters);

        /// <inheritdoc/>
        public async Task<int> ExecuteNonQueryAsync(string query) => await _queryProvider.ExecuteNonQueryAsync(query);

        /// <inheritdoc/>
        public async Task<int> ExecuteNonQueryAsync(string query, CommandParameterCollection parameters)
            => await _queryProvider.ExecuteNonQueryAsync(query, parameters);

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query">Arbitrary SQL query.</param>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result
        /// set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        public TResult? ExecuteScalar<TResult>(string query) where TResult : struct
            => ExecuteScalar<TResult>(query, new List<object>());

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query">Arbitrary SQL query.</param>
        /// <param name="args">The query parameters.</param>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result
        /// set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        public TResult? ExecuteScalar<TResult>(string query, params object[] args) where TResult : struct
            => Query<object>().Sql(query, args).ExecuteScalar<TResult>();

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <param name="query">Arbitrary SQL query.</param>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result
        /// set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        public string ExecuteScalar(string query) => ExecuteScalar(query, new List<object>());

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <param name="query">Arbitrary SQL query.</param>
        /// <param name="args">The query parameters.</param>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result
        /// set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        public string ExecuteScalar(string query, params object[] args)
            => Query<object>().Sql(query, args).ExecuteStringScalar();

        /// <inheritdoc cref="IQueryProvider.ExecuteStoredProcedure{TResult}(string)"/>
        public TResult ExecuteStoredProcedure<TResult>(string storedProcedureName)
            => _queryProvider.ExecuteStoredProcedure<TResult>(storedProcedureName);

        /// <inheritdoc cref="IQueryProvider.ExecuteStoredProcedure{TResult}(string, CommandParameterCollection)"/>
        public TResult ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters)
            => _queryProvider.ExecuteStoredProcedure<TResult>(storedProcedureName, parameters);

        /// <inheritdoc/>
        public ITransaction BeginTransaction()
            => _queryProvider.BeginTransaction(TransactionHelper.DefaultIsolationLevel);

        /// <inheritdoc/>
        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
            => _queryProvider.BeginTransaction(isolationLevel);

        /// <inheritdoc/>
        public void InitDatabaseForIdGenerator()
        {
            using (var idGenerator = _queryProvider.CreateIdGenerator("DummyTableName", 1))
            {
                idGenerator.InitDatabaseForIdGenerator();
            }
        }

        #endregion

        #region IDisposable

        private bool _disposedValue = false;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _queryProvider.Dispose();
                    _queryProvider = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion
    }
}
