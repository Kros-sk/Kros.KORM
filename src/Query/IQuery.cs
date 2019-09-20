using Kros.KORM.Metadata;
using Kros.KORM.Query.Sql;
using System;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe class for executing query.
    /// <para>
    /// Instance which implement this interface can be used for creating and executing query for T model.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    /// <seealso cref="Kros.KORM.Query.IQueryBase{T}" />
    /// <remarks>
    /// <para>
    /// When you don't use <c>Select</c> or <c>From</c> function, than default values are taken from model.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///     You can use standard string sql query for querying data.
    ///     <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Query data by sql" region="Sql" language="cs" />
    ///   </para>
    ///   <para>
    ///     You can use sql query builder.
    ///     <code source="..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Query data by query builder" region="Select" language="cs" />
    /// </para>
    /// </example>
    public interface IQuery<T> : ISelectionQuery<T>
    {
        /// <summary>
        /// Ignores the query filters defined over table by
        /// UseQueryFilter in <see cref="DatabaseConfigurationBase"/>.
        /// </summary>
        /// <returns>
        /// <see cref="ISelectionQuery{T}"/> for configuration query.
        /// </returns>
        ISelectionQuery<T> IgnoreQueryFilters();

        /// <summary>
        /// Create query from sql statement.
        /// </summary>
        /// <param name="sql">The SQL for executing in server.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        /// Query from sql.
        /// </returns>
        /// <remarks>
        /// Sql must be server specific. Because no translation is provide.
        /// </remarks>
        /// <exception cref="ArgumentNullException">if <c>sql</c> is null or white string.</exception>
        IQueryBase<T> Sql(RawSqlString sql, params object[] args);

        /// <summary>
        /// Create query from sql statement.
        /// </summary>
        /// <param name="sql">The SQL for executing in server.</param>
        /// <returns>
        /// Query from sql.
        /// </returns>
        /// <remarks>
        /// Sql must be server specific. Because no translation is provide.
        /// </remarks>
        /// <example>
        /// <code>
        /// var id = 15;
        /// var name = "Name";
        /// var items = query.Sql($"SELECT * FROM Table WHERE Id = {id} AND Name = {name}");
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">if <c>sql</c> is null or white string.</exception>
        IQueryBase<T> Sql(FormattableString sql);
    }
}
