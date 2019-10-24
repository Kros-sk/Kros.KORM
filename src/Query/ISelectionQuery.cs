using System;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface which describe query for calling selects methods.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IFromQuery{T}" />
    public interface ISelectionQuery<T> : IFromQuery<T>
    {
        /// <summary>
        /// Add columns to sql.
        /// </summary>
        /// <param name="columns">The columns for select clausule.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        ///  When Select method is not call, query take columns by T model.
        /// </remarks>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Projection" region="Select12" language="cs" />
        /// </example>
        IFromQuery<T> Select(params string[] columns);

        /// <summary>
        /// Add select part to sql.
        /// </summary>
        /// <param name="selectPart">The columns for select clausule. (Separate by ,)</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        ///  When <c>Select</c> method is not call, query take columns by T model.
        /// </remarks>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Projection" region="Select11" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>sqlPart</c> is null or white string.</exception>
        IFromQuery<T> Select(string selectPart);

        /// <summary>
        /// Add columns to sql
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        ///  When <c>Select</c> method is not call, query take columns by T model.
        /// </remarks>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Projection" region="Select13" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>selector</c> is null.</exception>
        IFromQuery<T> Select<TResult>(Func<T, TResult> selector);
    }
}
