using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent GROUP BY statement from sql select query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class GroupByExpression : QueryExpression
    {
        #region Constants

        /// <summary>
        /// The group by statement
        /// </summary>
        public const string GroupByStatement = "GROUP BY";
        private const string GroupByRegexPattern = @"GROUP\s+BY";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupByExpression"/> class.
        /// </summary>
        /// <param name="groupBy">The groupBy part of sql.</param>
        /// <remarks>
        /// Group by columns are separate by ,
        /// </remarks>
        public GroupByExpression(string groupBy)
        {
            Check.NotNullOrWhiteSpace(groupBy, nameof(groupBy));

            GroupByPart = Regex.Replace(groupBy, GroupByRegexPattern, string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupByExpression"/> class.
        /// </summary>
        /// <param name="groupBy">The groupBy.</param>
        public GroupByExpression(params string[] groupBy)
        {
            Check.NotNull(groupBy, nameof(groupBy));

            GroupByPart = string.Join(", ", groupBy);
        }

        /// <summary>
        /// Creates the GroupByExpression by selector.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// GroupByExpression
        /// </returns>
        public static GroupByExpression Create<T, TResult>(Func<T, TResult> selector) where T : new()
        {
            Check.NotNull(selector, nameof(selector));

            var select = selector(new T());

            var groupBy = select.GetType().GetProperties().Select(p => p.Name).ToArray();

            return new GroupByExpression(groupBy);
        }

        #endregion

        /// <summary>
        /// Gets or sets the group by part.
        /// </summary>
        public string GroupByPart { get; private set; }

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
                ? specificVisitor.VisitGroupBy(this)
                : CanReduce ? base.Accept(visitor) : this;
        }

        #endregion
    }
}
