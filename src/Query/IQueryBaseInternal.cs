using Kros.KORM.Metadata;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Sql;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe intenal funcions of Query.
    /// </summary>
    internal interface IQueryBaseInternal
    {
        /// <summary>
        /// Applies the query filter to this query.
        /// </summary>
        /// <param name="databaseMapper">Database mapper.</param>
        /// <param name="expressionVisitor">Sql expression visitior.</param>
        void ApplyQueryFilter(IDatabaseMapper databaseMapper, ISqlExpressionVisitor expressionVisitor);

        /// <summary>
        /// Gets a value indicating whether ignore query filters.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ignore query filters; otherwise, <c>false</c>.
        /// </value>
        bool IgnoreQueryFilters { get; }
    }
}
