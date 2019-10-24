using System;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface which describe query for calling <see cref="From(string)"/> method;
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <seealso cref="Kros.KORM.Query.IProjectionQuery{T}" />
    public interface IFromQuery<T> : IProjectionQuery<T>
    {
        /// <summary>
        /// Add FROM part to sql query.
        /// </summary>
        /// <param name="table">Table name or join.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        /// When <c>From</c> method is not call, query take table by T model type.
        /// </remarks>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="From table" region="From" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>table</c> is null or white string.</exception>
        IProjectionQuery<T> From(string table);
    }
}
