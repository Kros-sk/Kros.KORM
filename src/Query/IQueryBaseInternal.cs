using Kros.KORM.Query.Expressions;

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
        /// <param name="where">The query filter.</param>
        void ApplyQueryFilter(WhereExpression where);
    }
}
