using Kros.KORM.Query.Sql;
using System;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Represent result of projection operation.
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IQueryBase{T}" />
    public interface IProjectionQuery<T> : IQueryBase<T>
    {
        /// <summary>
        /// Add where condition to sql.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Projection" region="Where1" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        IFilteredQuery<T> Where(RawSqlString whereCondition, params object[] args);

        /// <summary>
        /// Add where condition to sql.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <example>
        /// <code>
        /// var item = query.Where($"Id = {1}")
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        IFilteredQuery<T> Where(FormattableString whereCondition);

        /// <summary>
        /// Returns the first item of which match where condition, or a default value if item doesn't exist.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// <see langword="null"/> if item doesn't exist; otherwise, the first item which match the condition.
        /// </returns>
        /// <example>
        /// <code>
        /// var item = query.FirstOrDefault("Id = @1", 22);
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        T FirstOrDefault(RawSqlString whereCondition, params object[] args);

        /// <summary>
        /// Returns the first item of which match where condition, or a default value if item doesn't exist.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <returns>
        /// <see langword="null"/> if item doesn't exist; otherwise, the first item which match the condition.
        /// </returns>
        /// <example>
        /// <code>
        /// var item = query.FirstOrDefault($"Id = {22}");
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        T FirstOrDefault(FormattableString whereCondition);

        /// <summary>
        /// Check if exist elements in the table which match condition.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">The arguments for where.</param>
        /// <returns>
        /// <see langword="true"/> if exist elements in the table which match condition; otherwise, false.
        /// </returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Check if exist elements in the table which match condition" region="Any" language="cs"  />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        bool Any(RawSqlString whereCondition, params object[] args);

        /// <summary>
        /// Check if exist elements in the table which match condition.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <returns>
        /// <see langword="true"/> if exist elements in the table which match condition; otherwise, false.
        /// </returns>
        /// <example>
        /// <code>
        /// var exist = database.Query&lt;Person&gt;().Any($"Age &gt; {18}");
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>whereCondition</c> is null or white string.</exception>
        bool Any(FormattableString whereCondition);

        /// <summary>
        /// Add order by statement to sql.
        /// </summary>
        /// <param name="orderBy">The order by statement.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="OrderBy" region="OrderBy" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>orderBy</c> is null or white string.</exception>
        IOrderedQuery<T> OrderBy(string orderBy);

        /// <summary>
        /// Add group by statement to sql query.
        /// </summary>
        /// <param name="groupBy">The group by statement.</param>
        /// <returns>
        /// Query for enumerable models.
        /// </returns>
        /// <remarks>
        /// You can also add HAVING statement.
        /// </remarks>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="GroupBy" region="GroupBy" language="cs" />
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>groupBy</c> is null or white string.</exception>
        IGroupedQuery<T> GroupBy(string groupBy);
    }
}
