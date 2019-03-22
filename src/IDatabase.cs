using Kros.Data.BulkActions;
using Kros.KORM.Data;
using Kros.KORM.Materializer;
using Kros.KORM.Query;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Kros.KORM
{
    /// <summary>
    /// Interface, which describe class for access to ORM features.
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
    /// <example>
    /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IModelBuilderExample.cs"
    ///       title="Materialize data table"
    ///       region="ModelBuilderExample"
    ///       language="cs" />
    /// </example>
    public interface IDatabase : IDisposable
    {
        /// <summary>
        /// Returns <see cref="DbProviderFactory"/> for current provider.
        /// </summary>
        DbProviderFactory DbProviderFactory { get; }

        /// <summary>
        /// Creates instance of <see cref="IBulkInsert"/>.
        /// </summary>
        /// <returns>Instance of <see cref="IBulkInsert"/>.</returns>
        IBulkInsert CreateBulkInsert();

        /// <summary>
        /// Creates instance of <see cref="IBulkUpdate"/>.
        /// </summary>
        /// <returns>Instance of <see cref="IBulkUpdate"/>.</returns>
        IBulkUpdate CreateBulkUpdate();

        /// <summary>
        /// Gets the model builder for materializing data from ado to models.
        /// </summary>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IModelBuilderExample.cs"
        ///       title="Materialize data table"
        ///       region="ModelBuilderExample"
        ///       language="cs" />
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs"
        ///       title="Query for obtaining data"
        ///       region="Select"
        ///       language="cs" />
        /// </example>
        IModelBuilder ModelBuilder { get; }

        /// <summary>
        /// Gets the query builder for T creating and executing query for obtains models.
        /// </summary>
        /// <typeparam name="T">Type of model, for which querying.</typeparam>
        /// <returns>Query builder.</returns>
        IQuery<T> Query<T>();

        /// <summary>
        /// Executes arbitrary query.
        /// </summary>
        /// <param name="query">Arbitrary SQL query. It should not be SELECT query.</param>
        /// <returns>
        /// Number of affected rows.
        /// </returns>
        int ExecuteNonQuery(string query);

        /// <summary>
        /// Executes arbitrary query with parameters.
        /// </summary>
        /// <param name="query">Arbitrary SQL query. It should not be SELECT query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>
        /// Number of affected rows.
        /// </returns>
        int ExecuteNonQuery(string query, CommandParameterCollection parameters);

        /// <summary>
        /// Asynchronously executes arbitrary query.
        /// </summary>
        /// <param name="query">Arbitrary SQL query. It should not be SELECT query.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the
        /// numbers of affected rows.
        /// </returns>
        Task<int> ExecuteNonQueryAsync(string query);

        /// <summary>
        /// Asynchronously executes arbitrary query with parameters.
        /// </summary>
        /// <param name="query">Arbitrary SQL query. It should not be SELECT query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the
        /// numbers of affected rows.
        /// </returns>
        Task<int> ExecuteNonQueryAsync(string query, CommandParameterCollection parameters);

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
        TResult? ExecuteScalar<TResult>(string query) where TResult : struct;

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
        TResult? ExecuteScalar<TResult>(string query, params object[] args) where TResult : struct;

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <param name="query">Arbitrary SQL query.</param>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result
        /// set is empty. Returns a maximum of 2033 characters.
        /// </returns>
        string ExecuteScalar(string query);

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
        string ExecuteScalar(string query, params object[] args);

        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <returns>
        /// Result of the stored procedure.
        /// </returns>
        TResult ExecuteStoredProcedure<TResult>(string storedProcedureName);

        /// <summary>
        /// Executes the stored procedure with parameters.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <param name="parameters">The stored procedure parameters.</param>
        /// <returns>
        /// Result of the stored procedure.
        /// </returns>
        TResult ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters);

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns><see cref="ITransaction"/> wrapping access to the underlying store's transaction object.</returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\TransactionExample.cs" title="Transaction" region="Transaction" language="cs" />
        /// </example>
        ITransaction BeginTransaction();

        /// <summary>
        /// Begins the transaction using the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The database isolation level with which the underlying store transaction will be created.</param>
        /// <returns><see cref="ITransaction"/> wrapping access to the underlying store's transaction object.</returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\TransactionExample.cs" title="Transaction" region="TransactionsIsolationLevel" language="cs" />
        /// </example>
        ITransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Initializes database for using ID generator. Initialization can mean creating necessary table and stored procedure.
        /// </summary>
        void InitDatabaseForIdGenerator();
    }
}
