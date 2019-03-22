using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe class for executing query.
    /// <para>
    /// Instance which implement this interface can be used for creating and executing query for T model.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of model class.</typeparam>
    public interface IQueryBase<T> : IEnumerable<T>,
        System.Linq.IQueryable<T>, System.Linq.IQueryable, System.Linq.IOrderedQueryable<T>, System.Linq.IOrderedQueryable
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
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
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
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
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
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\IQueryExample.cs" title="Execute scalar" region="ExecuteScalar" language="cs" />
        /// </example>
        TRet? ExecuteScalar<TRet>() where TRet : struct;
    }
}
