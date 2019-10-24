using Kros.KORM.Metadata;
using Kros.KORM.Query.Sql;

namespace Kros.KORM.Query
{
    /// <summary>
    /// Interface, which describe intenal functions of Query.
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
        /// Gets a value indicating whether to ignore query filter.
        /// </summary>
        /// <value>
        ///   <c>true</c> if query filter is ignored; otherwise, <c>false</c>.
        /// </value>
        bool IgnoreQueryFilters { get; }
    }
}
