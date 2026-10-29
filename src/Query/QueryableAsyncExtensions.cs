using Kros.KORM.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Asynchronous extension methods for <see cref="IQueryable{T}"/> queries created by KORM.
    /// </summary>
    /// <remarks>
    /// The methods apply to queries created by KORM (for example by <c>IDatabase.Query&lt;T&gt;()</c>). They throw
    /// <see cref="InvalidOperationException"/> when the queryable is not a KORM query.
    /// </remarks>
    public static class QueryableAsyncExtensions
    {
        /// <summary>
        /// Executes the query and returns the result as a list.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>List of all items returned by the query.</returns>
        /// <remarks>
        /// The method applies to queries created by KORM. It throws <see cref="InvalidOperationException"/>
        /// otherwise.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not a KORM query.</exception>
        public static Task<List<T>> ToListAsync<T>(
            this IQueryable<T> source,
            CancellationToken cancellationToken = default)
            => GetQuery(source).ToListAsync(cancellationToken);

        /// <summary>
        /// Executes the query and returns the first item, or default value when the query returns no item.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The first item, or default value when the query returns no item.</returns>
        /// <remarks>
        /// The method applies to queries created by KORM. It throws <see cref="InvalidOperationException"/>
        /// otherwise.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not a KORM query.</exception>
        public static Task<T> FirstOrDefaultAsync<T>(
            this IQueryable<T> source,
            CancellationToken cancellationToken = default)
            => GetQuery(source).FirstOrDefaultAsync(cancellationToken);

        /// <summary>
        /// Determines whether the query returns any item.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> if the query returns any item; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// The method applies to queries created by KORM. It throws <see cref="InvalidOperationException"/>
        /// otherwise.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not a KORM query.</exception>
        public static Task<bool> AnyAsync<T>(
            this IQueryable<T> source,
            CancellationToken cancellationToken = default)
            => GetQuery(source).AnyAsync(cancellationToken);

        /// <summary>
        /// Executes the query and returns the result as an asynchronous sequence.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Asynchronous sequence of items returned by the query.</returns>
        /// <remarks>
        /// The method applies to queries created by KORM. It throws <see cref="InvalidOperationException"/>
        /// otherwise.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not a KORM query.</exception>
        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(
            this IQueryable<T> source,
            CancellationToken cancellationToken = default)
            => GetQuery(source).AsAsyncEnumerable(cancellationToken);

        private static IQueryBase<T> GetQuery<T>(IQueryable<T> source)
            => source is IQueryBase<T> query
                ? query
                : throw new InvalidOperationException(Resources.QueryableIsNotKormQuery);
    }
}
