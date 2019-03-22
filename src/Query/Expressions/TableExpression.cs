using Kros.KORM.Query.Sql;
using Kros.Utils;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent FROM statement from sql select query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class TableExpression : QueryExpression
    {
        /// <summary>
        /// From statement
        /// </summary>
        public const string FromStatement = "FROM";

        /// <summary>
        /// Initializes a new instance of the <see cref="TableExpression"/> class.
        /// </summary>
        /// <param name="table">The table (or join).</param>
        public TableExpression(string table)
        {
            Check.NotNullOrWhiteSpace(table, nameof(table));

            TablePart = Regex.Replace(table, FromStatement, string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Gets the table part.
        /// </summary>
        public string TablePart { get; private set; }

        #region Visitor

        /// <summary>
        /// Dispatches to the specific visit method for this node type. For example,
        /// <see cref="T:System.Linq.Expressions.MethodCallExpression"/> calls the
        /// <see cref="M:System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)"/>.
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
                ? specificVisitor.VisitTable(this)
                : CanReduce ? base.Accept(visitor) : this;
        }

        #endregion
    }
}
