using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Represents result of grouping operation.
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IQueryBase{T}" />
    public interface IGroupedQuery<T> : IQueryBase<T>
    {
        /// <summary>
        /// Add order by statement to sql.
        /// </summary>
        /// <param name="orderBy">The order by statement.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="OrderBy" region="OrderBy" language="cs"  />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>orderBy</c> is null or white string.</exception>
        IOrderedQuery<T> OrderBy(string orderBy);
    }
}
