using Kros.Data;
using Kros.Data.BulkActions;
using Kros.KORM.Data;
using Kros.KORM.Materializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface for provider, which know execute query.
    /// </summary>
    public interface IQueryProvider : System.Linq.IQueryProvider, IDisposable
    {
        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T">Type of model result.</typeparam>
        /// <param name="query">The query.</param>
        /// <exception cref="ArgumentNullException">If query is null.</exception>
        /// <returns>
        /// IEnumerable of models, which was materialized by query.
        /// </returns>
        IEnumerable<T> Execute<T>(IQuery<T> query);

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>
        /// The first column of the first row in the result set, or a <see langword="null"/> if the result set is empty.
        /// Returns a maximum of 2033 characters.
        /// </returns>
        object ExecuteScalar<T>(IQuery<T> query);

        /// <summary>
        /// Asynchronously executes action in transaction.
        /// </summary>
        /// <param name="action">Action which will be executed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task ExecuteInTransactionAsync(Func<Task> action);

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>Number of affected rows.</returns>
        int ExecuteNonQueryCommand(IDbCommand command);

        /// <summary>
        /// Asynchronously executes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation. The task result contains the
        /// numbers of affected rows.
        /// </returns>
        Task<int> ExecuteNonQueryCommandAsync(DbCommand command);

        /// <summary>
        /// Returns <see cref="DbProviderFactory"/> for current provider.
        /// </summary>
        DbProviderFactory DbProviderFactory { get; }

        /// <summary>
        /// Returns, if provider supports peparing of command (<see cref="DbCommand.Prepare"/>).
        /// </summary>
        /// <returns><see langword="true"/> is provider supports preparing command, otherwise <see langword="false"/>.</returns>
        bool SupportsPrepareCommand();

        /// <summary>
        /// Sets correct data type to <paramref name="parameter"/>, according to column <paramref name="columnName"/>
        /// in table <paramref name="tableName"/>. The method does not set general <see cref="DbParameter.DbType"/>,
        /// but specific for given database (<c>SqlParameter.SqlDbType</c>, <c>OleDbParameter.OleDbType</c>).
        /// </summary>
        /// <param name="parameter">The parameter to which the data type is set.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name which data type is obtained.</param>
        void SetParameterDbType(DbParameter parameter, string tableName, string columnName);

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
        /// Vytvorí inicializovaný príkaz <see cref="DbCommand"/>, pre aktuálnu transakciu.
        /// Používa sa iba v rámci volania <see cref="ExecuteInTransactionAsync(Func{Task})"/>.
        /// </summary>
        /// <returns>Inicializovaný príkaz.</returns>
        DbCommand GetCommandForCurrentTransaction();

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
        /// Executes the stored procedure with parameters and returns its result. The result can be scalar value
        /// (primitive or complex &#8211; class type), or a list of values
        /// (<see cref="IEnumerable{T}">IEnumerable&lt;T&gt;</see>).
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result. It can be scalar primitive or complex (class) value,
        /// or <see cref="IEnumerable{T}"/> of some value. If the <c>TResult</c> is primitive scalar value,
        /// the result is converted to that. If <c>TResult</c> is a classs, the result of stored procedure
        /// is converted (materialized) to that class type. If <c>TResult</c> is <see cref="IEnumerable{T}"/>,
        /// the result of procedure is converted (materialized) to that.
        /// </typeparam>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <returns>
        /// Result of the stored procedure.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <list type="bullet">
        /// <item>
        /// Result type <typeparamref name="TResult"/> is non-generic <see cref="IEnumerable"/>. If it is
        /// <see cref="IEnumerable"/>, it must be generic <see cref="IEnumerable{T}"/>.
        /// </item>
        /// <item>
        /// Instance of <see cref="IModelBuilder"/> initialized in constructor does not have method with signature
        /// <c>Materialize(IDataReader)</c>.
        /// </item>
        /// </list>
        /// </exception>
        /// <example>
        /// <para>
        /// For the examples, we expect to have a <see cref="Database"/> initialized and a <c>Person</c> class defined.
        /// </para>
        /// <code
        ///   source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\ExecuteStoredProcedureExamples.cs"
        ///   title="ExecuteStoredProcedure assumptions"
        ///   region="Init"
        ///   language="cs" />
        /// <code
        ///   source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\ExecuteStoredProcedureExamples.cs"
        ///   title="ExecuteStoredProcedure examples"
        ///   region="Examples"
        ///   language="cs" />
        /// </example>
        TResult ExecuteStoredProcedure<TResult>(string storedProcedureName);

        /// <inheritdoc cref="ExecuteStoredProcedure{TResult}(string)"/>
        /// <param name="storedProcedureName">Name of the stored procedure.</param>
        /// <param name="parameters">The stored procedure parameters. Values of output parameters
        /// (<see cref="ParameterDirection.Output">ParameterDirection.Output</see>
        /// <see cref="ParameterDirection.InputOutput">ParameterDirection.InputOutput</see>)
        /// are set back to corresponding parameter in collection</param>
        /// <exception cref="ArgumentException">
        /// Value of any of the parameters in <paramref name="parameters"/> is <see langword="null"/> or <see cref="DBNull"/>
        /// and its data type (<see cref="CommandParameter.DataType"/>) is not set.
        /// </exception>
        TResult ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters);

        /// <summary>
        /// Begins the transaction using the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The database isolation level with which the underlying store transaction will be created.</param>
        /// <returns><see cref="ITransaction"/> wrapping access to the underlying store's transaction object.</returns>
        ITransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Creates the identifier generator.
        /// </summary>
        /// <param name="tableName">Name of the database table.</param>
        /// <param name="batchSize">Size of inserting the batch.</param>
        /// <returns>The identifier generator.</returns>
        IIdGenerator CreateIdGenerator(string tableName, int batchSize);
    }
}
