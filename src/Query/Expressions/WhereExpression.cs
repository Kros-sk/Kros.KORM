using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent WHERE statement from sql select query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class WhereExpression : ArgsExpression
    {
        /// <summary>
        /// Where statement
        /// </summary>
        public const string WhereStatement = "WHERE";

        /// <summary>
        /// Initializes a new instance of the <see cref="TableExpression"/> class.
        /// </summary>
        /// <param name="whereCondition">The where condition.</param>
        /// <param name="args">Where args.</param>
        public WhereExpression(RawSqlString whereCondition, params object[] args)
        {
            Check.NotNullOrWhiteSpace(whereCondition.Format, nameof(whereCondition));

            whereCondition = whereCondition.Format.Trim();
            if (whereCondition.Format.StartsWith(WhereStatement, StringComparison.InvariantCultureIgnoreCase))
            {
                whereCondition = whereCondition.Format.Substring(WhereStatement.Length).TrimStart();
            }

            Sql = whereCondition.Format;

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
                ? specificVisitor.VisitWhere(this)
                : CanReduce ? base.Accept(visitor) : this;
        }

        #endregion
    }
}
