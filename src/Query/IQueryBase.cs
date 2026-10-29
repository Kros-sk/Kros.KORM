using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe class for executing query.
    /// <para>
    /// Instance which implement this interface can be used for creating and executing query for T model.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    public interface IQueryBase<T> : IEnumerable<T>, System.Linq.IQueryable<T>, System.Linq.IQueryable,
        System.Linq.IOrderedQueryable<T>, System.Linq.IOrderedQueryable
    {
        /// <summary>
        /// Returns the collection of all entities that can be queried from the database.
        /// </summary>
        /// <returns><seealso cref="DbSet{T}"/>.</returns>
        IDbSet<T> AsDbSet();

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set, or <see langword="null"/> if the result set is empty.
        /// Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
        /// </example>
        object ExecuteScalar();

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set as string, or <see langword="null"/> if the result set is empty.
        /// Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
        /// </example>
        string ExecuteStringScalar();

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <typeparam name="TRet">Return type.</typeparam>
        /// <returns>
        /// The first column of the first row in the result set as nullable type of TRet. If the result set is empty, then HasValue is false.
        /// Returns a maximum of 2033 characters.
        /// </returns>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
        /// </example>
        TRet? ExecuteScalar<TRet>() where TRet : struct;

        /// <summary>
        /// Returns the query result as an asynchronous sequence. The query is executed when the sequence is enumerated
        /// and rows are read asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Asynchronous sequence of materialized items.</returns>
        IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously executes the query and returns all materialized items in a list.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task whose result is the list of all items returned by the query.</returns>
        Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously executes the query and returns the first item, or the default value if the result is empty.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task whose result is the first item, or <see langword="default"/> if the query returns no rows.
        /// </returns>
        Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously determines whether the query returns any row.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task whose result is <see langword="true"/> if the query returns at least one row; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously executes the query and returns the first column of the first row. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task whose result is the first column of the first row in the result set,
        /// or <see langword="null"/> if the result set is empty.
        /// </returns>
        Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously executes the query and returns the first column of the first row as string,
        /// or <see langword="null"/> if the result is empty.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task whose result is the first column of the first row in the result set as string,
        /// or <see langword="null"/> if the result set is empty.
        /// </returns>
        Task<string> ExecuteStringScalarAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously executes the query and returns the first column of the first row as nullable
        /// <typeparamref name="TRet"/>; HasValue is false if the result is empty.
        /// </summary>
        /// <typeparam name="TRet">Return type.</typeparam>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task whose result is the first column of the first row in the result set as nullable type of TRet.
        /// If the result set is empty, then HasValue is false.
        /// </returns>
        Task<TRet?> ExecuteScalarAsync<TRet>(CancellationToken cancellationToken = default) where TRet : struct;
    }
}
