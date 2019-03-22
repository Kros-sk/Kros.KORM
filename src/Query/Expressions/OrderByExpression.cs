using Kros.KORM.Query.Sql;
using Kros.Utils;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent ORDER BY statement from sql select query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class OrderByExpression : QueryExpression
    {
        #region Constants

        /// <summary>
        /// The group by statement
        /// </summary>
        public const string OrderByStatement = "ORDER BY";
        private const string OrderByRegexPattern = @"ORDER\s+BY";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByExpression"/> class.
        /// </summary>
        /// <param name="orderBy">The orderBy part of sql.</param>
        /// <remarks>
        /// Order by columns are separate by ,
        /// </remarks>
        public OrderByExpression(string orderBy)
        {
            Check.NotNullOrWhiteSpace(orderBy, nameof(orderBy));

            OrderByPart = Regex.Replace(orderBy, OrderByRegexPattern, string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByExpression"/> class.
        /// </summary>
        /// <param name="columns">The orderBy.</param>
        public OrderByExpression(params string[] columns)
        {
            Check.NotNull(columns, nameof(columns));

            OrderByPart = string.Join(", ", columns);
        }

        #endregion

        /// <summary>
        /// Gets or sets the group by part.
        /// </summary>
        public string OrderByPart { get; private set; }

        #region Visitor

        /// <summary>
        /// Dispatches to the specific visit method for this node type. For example, <see cref="T:System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="M:System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.
        /// </summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>
        /// The result of visiting this node.
        /// </returns>
        protected override Expression Accept(ExpressionVisitor visitor)
        {
            Check.NotNull(visitor, nameof(visitor));

            var specificVisitor = visitor as ISqlExpressionVisitor;

            return specificVisitor != null
                ? specificVisitor.VisitOrderBy(this)
                : CanReduce ? base.Accept(visitor) : this;
        }

        #endregion
    }
}
