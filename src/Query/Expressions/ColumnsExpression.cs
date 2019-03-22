using Kros.KORM.Metadata;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression which represent projection part of sql select.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class ColumnsExpression : QueryExpression
    {
        /// <summary>
        /// The select statement
        /// </summary>
        private const string SelectStatement = "SELECT";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnsExpression"/> class.
        /// </summary>
        /// <param name="columns">The columns part of sql.</param>
        /// <remarks>
        /// Columns are separate by ,
        /// </remarks>
        public ColumnsExpression(string columns)
        {
            Check.NotNullOrWhiteSpace(columns, nameof(columns));

            ColumnsPart = Regex.Replace(columns, SelectStatement, string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnsExpression"/> class.
        /// </summary>
        /// <param name="columns">The columns.</param>
        public ColumnsExpression(params string[] columns)
        {
            Check.NotNull(columns, nameof(columns));

            ColumnsPart = string.Join(", ", columns);
        }

        /// <summary>
        /// Creates the ColumnsExpression by selector.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <param name="tableInfo">The table information.</param>
        /// <returns>
        /// ColumnsExpression
        /// </returns>
        public static ColumnsExpression Create<T, TResult>(Func<T, TResult> selector, TableInfo tableInfo)
        {
            Check.NotNull(selector, nameof(selector));

            var select = selector((T)Activator.CreateInstance(typeof(T)));
            var columns = select.GetType().GetProperties().Select(p => tableInfo.GetColumnInfo(p).Name).ToArray();

            return new ColumnsExpression(columns);
        }

        #endregion

        /// <summary>
        /// Gets the columns part.
        /// </summary>
        public string ColumnsPart { get; private set; }

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
                ? specificVisitor.VisitColumns(this)
                : CanReduce ? base.Accept(visitor) : this;
        }

        #endregion
    }
}
