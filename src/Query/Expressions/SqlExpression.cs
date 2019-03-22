using Kros.KORM.Query.Sql;
using Kros.Utils;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent sql query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class SqlExpression : ArgsExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExpression"/> class.
        /// </summary>
        /// <param name="sqlQuery">The SQL query.</param>
        /// <param name="args">Where args.</param>
        public SqlExpression(RawSqlString sqlQuery, params object[] args)
        {
            Check.NotNullOrWhiteSpace(sqlQuery.Format, nameof(sqlQuery));

            Sql = sqlQuery.Format.Trim();
            Parameters = args.ToList();
        }

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
                ? specificVisitor.VisitSql(this)
                : base.Accept(visitor);
        }

        #endregion
    }
}
