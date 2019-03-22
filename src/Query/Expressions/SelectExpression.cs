using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Expressions
{
    /// <summary>
    /// Expression, which represent SELECT query.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class SelectExpression : QueryExpression
    {
        #region Private fields

        private TableInfo _tableInfo;
        private ColumnsExpression _columnsExpression;
        private TableExpression _tableExpression;
        private object _originalQuery;

        #endregion

        /// <summary>
        /// The select statement
        /// </summary>
        public const string SelectStatement = "SELECT";

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectExpression"/> class.
        /// </summary>
        /// <param name="tableInfo">The table info.</param>
        public SelectExpression(TableInfo tableInfo)
        {
            Check.NotNull(tableInfo, nameof(tableInfo));

            _tableInfo = tableInfo;
        }

        #endregion

        #region Select parts

        /// <summary>
        /// Gets the columns expression.
        /// </summary>
        public ColumnsExpression ColumnsExpression
        {
            get
            {
                if (_columnsExpression == null)
                {
                    var columns = _tableInfo.Columns.Select(p => p.Name).ToArray();
                    _columnsExpression = new ColumnsExpression(columns);
                }

                return _columnsExpression;
            }
        }

        /// <summary>
        /// Gets the table expression.
        /// </summary>
        public TableExpression TableExpression
        {
            get
            {
                if (_tableExpression == null)
                {
                    _tableExpression = new TableExpression(_tableInfo.Name);
                }
                return _tableExpression;
            }
        }

        /// <summary>
        /// Gets or sets the where expression.
        /// </summary>
        public WhereExpression WhereExpression { get; private set; }

        /// <summary>
        /// Gets or sets the group by expression.
        /// </summary>
        public GroupByExpression GroupByExpression { get; private set; }

        /// <summary>
        /// Gets or sets the order by expression.
        /// </summary>
        public OrderByExpression OrderByExpression { get; private set; }

        #endregion

        #region Adding select query parts

        /// <summary>
        /// Sets the columns expression.
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <exception cref="System.ArgumentException">'columnExpression' can be applied only once.;columnsExpression</exception>
        public void SetColumnsExpression(ColumnsExpression columnExpression)
        {
            Check.NotNull(columnExpression, nameof(columnExpression));
            _columnsExpression = columnExpression;
        }

        /// <summary>
        /// Sets the table expression.
        /// </summary>
        /// <param name="tableExpression">The table expression.</param>
        /// <exception cref="System.ArgumentException">'tableExpression' can be applied only once.;tableExpression</exception>
        public void SetTableExpression(TableExpression tableExpression)
        {
            Check.NotNull(tableExpression, nameof(tableExpression));
            if (_tableExpression != null)
            {
                throw new ArgumentException(
                    string.Format(Resources.ExpressionCanBeAppliedOnlyOnce, nameof(TableExpression)), nameof(tableExpression));
            }
            _tableExpression = tableExpression;
        }

        /// <summary>
        /// Sets the where expression.
        /// </summary>
        /// <param name="whereExpression">The where expression.</param>
        /// <exception cref="System.ArgumentException">'whereExpression' can be applied only once.;whereExpression</exception>
        public void SetWhereExpression(WhereExpression whereExpression)
        {
            Check.NotNull(whereExpression, nameof(whereExpression));
            if (WhereExpression != null)
            {
                throw new ArgumentException(
                    string.Format(Resources.ExpressionCanBeAppliedOnlyOnce, nameof(WhereExpression)), nameof(whereExpression));
            }
            WhereExpression = whereExpression;
        }

        /// <summary>
        /// Sets the group by expression.
        /// </summary>
        /// <param name="groupByExpression">The group by expression.</param>
        /// <exception cref="System.ArgumentException">'groupByExpression' can be applied only once.;groupByExpression</exception>
        public void SetGroupByExpression(GroupByExpression groupByExpression)
        {
            Check.NotNull(groupByExpression, nameof(groupByExpression));
            if (GroupByExpression != null)
            {
                throw new ArgumentException(string.Format(Resources.ExpressionCanBeAppliedOnlyOnce, nameof(GroupByExpression)),
                    nameof(groupByExpression));
            }
            GroupByExpression = groupByExpression;
        }

        /// <summary>
        /// Sets the order by expression.
        /// </summary>
        /// <param name="orderByExpression">The order by expression.</param>
        /// <exception cref="System.ArgumentException">'orderByExpression' can be applied only once.;orderByExpression</exception>
        public void SetOrderByExpression(OrderByExpression orderByExpression)
        {
            Check.NotNull(orderByExpression, nameof(orderByExpression));
            if (OrderByExpression != null)
            {
                throw new ArgumentException(string.Format(Resources.ExpressionCanBeAppliedOnlyOnce, nameof(OrderByExpression)),
                    nameof(orderByExpression));
            }
            OrderByExpression = orderByExpression;
        }

        #endregion

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
                ? specificVisitor.VisitSelect(this)
                : base.Accept(visitor);
        }

        /// <summary>
        /// Reduces the node and then calls the visitor delegate on the reduced expression. The method throws an exception if the node is not reducible.
        /// </summary>
        /// <param name="visitor">An instance of <see cref="T:System.Func`2" />.</param>
        /// <returns>
        /// The expression being visited, or an expression which should replace it in the tree.
        /// </returns>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            visitor.Visit(ColumnsExpression.Reduce());

            visitor.Visit(TableExpression);

            if (WhereExpression != null)
            {
                visitor.Visit(WhereExpression);
            }

            if (GroupByExpression != null)
            {
                visitor.Visit(GroupByExpression);
            }

            if (OrderByExpression != null)
            {
                visitor.Visit(OrderByExpression);
            }

            return this;
        }
        #endregion

        #region Linq

        /// <summary>
        /// Create constant expression for linq.
        /// </summary>
        /// <typeparam name="T">Type of entity.</typeparam>
        /// <param name="tableInfo">The table information.</param>
        /// <param name="query">The query.</param>
        /// <returns>Constant expresion.</returns>
        internal static Expression Constant<T>(TableInfo tableInfo, Query<T> query)
        {
            var expression = new SelectExpression(tableInfo);
            expression._originalQuery = query;

            return expression;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression"></see> represents.
        /// </summary>
        public override Type Type => _originalQuery.GetType();

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value => _originalQuery;

        /// <summary>
        /// Gets the node type of this <see cref="T:System.Linq.Expressions.Expression"></see>.
        /// </summary>
        public sealed override ExpressionType NodeType => ExpressionType.Constant;

        #endregion
    }
}
