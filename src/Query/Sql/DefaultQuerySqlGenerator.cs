using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// Default sql query visitor for generating SELECT command.
    /// </summary>
    /// <seealso cref="Kros.KORM.Query.Sql.ISqlExpressionVisitor" />
    public class DefaultQuerySqlGenerator : ExpressionVisitor, ISqlExpressionVisitor
    {
        /// <summary>
        /// SQL Server boolean value <see langword="true"/>.
        /// </summary>
        protected const string SqlTrue = "1";

        /// <summary>
        /// SQL Server boolean value <see langword="false"/>.
        /// </summary>
        protected const string SqlFalse = "0";

        private const string VbOperatorsClassName = "Microsoft.VisualBasic.CompilerServices.Operators";
        private const string VbEmbeddedOperatorsClassName = "Microsoft.VisualBasic.CompilerServices.EmbeddedOperators";

        private StringBuilder _sqlBuilder;
        private int _top;
        private int _topPosition;
        private int _columnsPosition;
        private int _skip;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="databaseMapper">Database mapper.</param>
        public DefaultQuerySqlGenerator(IDatabaseMapper databaseMapper)
        {
            Check.NotNull(databaseMapper, nameof(databaseMapper));

            DatabaseMapper = databaseMapper;
        }

        /// <summary>
        /// Gets the database mapper.
        /// </summary>
        protected IDatabaseMapper DatabaseMapper { get; private set; }

        /// <summary>
        /// Generates the SQL from expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>SQL select command text.</returns>
        public virtual QueryInfo GenerateSql(Expression expression)
        {
            Check.NotNull(expression, nameof(expression));
            _wasAny = false;
            _sqlBuilder = new StringBuilder();
            _top = 0;
            _topPosition = 0;
            _skip = 0;
            _columnsPosition = 0;
            LinqParameters = new Parameters();
            Orders.Clear();
            MemberExpressionStack.Clear();

            Visit(expression);

            CheckSkip();

            AddOrderBy();
            AddAnyMethod();
            AddLimitAndOffset();

            return new QueryInfo(SqlBuilder.ToString(), CreateQueryReader());
        }

        /// <inheritdoc />
        public WhereExpression GenerateWhereCondition(Expression whereExpression, string parameterNamePrefix = "")
        {
            Check.NotNull(whereExpression, nameof(whereExpression));

            LinqParameters = new Parameters(parameterNamePrefix);
            LinqStringBuilder = new StringBuilder();
            MemberExpressionStack.Clear();

            Visit(whereExpression);

            return new WhereExpression(LinqStringBuilder.ToString(), LinqParameters.GetParams());
        }

        /// <summary>
        /// List of ORDER BY parts for the query.
        /// </summary>
        protected List<string> Orders { get; } = new List<string>();

        /// <summary>
        /// General builder for the SQL query.
        /// </summary>
        protected StringBuilder SqlBuilder => _sqlBuilder;

        /// <summary>
        /// Offset - the number of rows to skip.
        /// </summary>
        protected int Skip => _skip;

        /// <summary>
        /// Limit - the maximum number of rows to return.
        /// </summary>
        protected int Top => _top;

        /// <summary>
        /// Position in the SQL query, where TOP clause belongs.
        /// </summary>
        protected int TopPosition => _topPosition;

        /// <summary>
        /// Position in the SQL Query, where columns part ends. Some other columns can be added here.
        /// </summary>
        protected int ColumnsPosition => _columnsPosition;

        private void CheckSkip()
        {
            if ((_skip > 0) && (Orders.Count == 0))
            {
                throw new InvalidOperationException(Resources.SkipWithoutOrderByInQuery);
            }
        }

        /// <summary>
        /// Creates a reader over data, for example to manually apply limit and offset if the database does not support it.
        /// </summary>
        /// <returns>Implementation of <see cref="IDataReaderEnvelope"/> or <see langword="null"/>.</returns>
        protected virtual IDataReaderEnvelope CreateQueryReader() => _skip > 0 ? new LimitOffsetDataReader(Top, Skip) : null;

        /// <summary>
        /// Adds ORDER BY clause to the query.
        /// </summary>
        protected virtual void AddOrderBy()
        {
            if (Orders.Count > 0)
            {
                SqlBuilder.Append(' ');
                SqlBuilder.Append(CreateOrderByString());
            }
        }

        /// <summary>
        /// Creates ORDER BY string.
        /// </summary>
        /// <returns>String.</returns>
        protected string CreateOrderByString()
            => Orders.Count == 0
            ? string.Empty
            : OrderByExpression.OrderByStatement + " " + string.Join(", ", Orders.AsEnumerable().Reverse());

        /// <summary>
        /// Adds limit (Top) and offset (Skip) clauses to the query.
        /// </summary>
        protected virtual void AddLimitAndOffset()
        {
            if ((_top > 0) && (_skip == 0))
            {
                SqlBuilder.Insert(_topPosition, $"TOP {_top} ");
                _top = 0;
            }
        }

        private void AddAnyMethod()
        {
            if (_wasAny)
            {
                _sqlBuilder = new StringBuilder(BindAnyCondition(SqlBuilder.ToString()));
            }
        }

        /// <summary>
        /// Adds any method to query.
        /// </summary>
        protected virtual string BindAnyCondition(string existsCondition)
            => $"SELECT (CASE WHEN EXISTS({existsCondition}) THEN 1 ELSE 0 END)";

        /// <summary>
        /// Visits the SQL.
        /// </summary>
        /// <param name="sqlExpression">The SQL expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitSql(SqlExpression sqlExpression)
        {
            SqlBuilder.Append(sqlExpression.Sql);

            return sqlExpression;
        }

        /// <summary>
        /// Visits the select.
        /// </summary>
        /// <param name="selectExpression">The select expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitSelect(SelectExpression selectExpression)
        {
            SqlBuilder.Append(SelectExpression.SelectStatement);
            SqlBuilder.Append(' ');

            if (_top > 0)
            {
                _topPosition = SqlBuilder.Length;
            }

            VisitExtension(selectExpression);

            return selectExpression.Reduce();
        }

        /// <summary>
        /// Visits the columns.
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitColumns(ColumnsExpression columnExpression)
        {
            SqlBuilder.Append(columnExpression.ColumnsPart);
            _columnsPosition = SqlBuilder.Length;
            return columnExpression;
        }

        /// <summary>
        /// Visits the table.
        /// </summary>
        /// <param name="tableExpression">The table expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitTable(TableExpression tableExpression)
        {
            SqlBuilder.AppendFormat(" {0} {1}", TableExpression.FromStatement, tableExpression.TablePart);

            return tableExpression;
        }

        /// <summary>
        /// Visits the where.
        /// </summary>
        /// <param name="whereExpression">The where expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitWhere(WhereExpression whereExpression)
        {
            SqlBuilder.AppendFormat(" {0} ({1})", WhereExpression.WhereStatement, whereExpression.Sql);

            return whereExpression;
        }

        /// <summary>
        /// Visits the group by.
        /// </summary>
        /// <param name="groupByExpression">The group by expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitGroupBy(GroupByExpression groupByExpression)
        {
            SqlBuilder.AppendFormat(" {0} {1}", GroupByExpression.GroupByStatement, groupByExpression.GroupByPart);

            return groupByExpression;
        }

        /// <summary>
        /// Visits the order by.
        /// </summary>
        /// <param name="orderByExpression">The order by expression.</param>
        /// <returns>
        /// Expression
        /// </returns>
        public virtual Expression VisitOrderBy(OrderByExpression orderByExpression)
        {
            Orders.Add(orderByExpression.OrderByPart);
            return orderByExpression;
        }

        #region LINQ

        /// <summary>
        /// Returns the last <see cref="MemberExpression"/> node in the <see cref="MemberExpressionStack"/> or
        /// <see langword="null"/> if the stack is empty.
        /// </summary>
        protected MemberExpression PreviousMemberExpression
            => MemberExpressionStack.Count == 0 ? null : MemberExpressionStack.Peek();

        /// <summary>
        /// Stack for <see cref="MemberExpression"/> nodes.
        /// </summary>
        protected Stack<MemberExpression> MemberExpressionStack { get; } = new Stack<MemberExpression>();

        /// <summary>
        /// Get root select expression.
        /// </summary>
        protected SelectExpression SelectExpression { get; private set; }

        /// <summary>
        /// Gets the linq string builder.
        /// </summary>
        protected StringBuilder LinqStringBuilder { get; private set; }

        /// <summary>
        /// Adds new parameter to the SQL with specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the SQL parameter.</param>
        protected void AddParameterWithValue(object value)
        {
            LinqStringBuilder.Append(LinqParameters.GetNextParamName());
            LinqParameters.AddParameter(value);
        }

        /// <summary>
        /// Gets the linq query parameters.
        /// </summary>
        protected Parameters LinqParameters { get; private set; }

        private string GetColumnNameFromMember(MemberExpression node)
        {
            TableInfo tableInfo = DatabaseMapper.GetTableInfo(node.Member.DeclaringType);
            ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(node.Member.Name);

            return tableInfo.Delimiters.QuoteIdentifier(columnInfo.Name);
        }

        private void AppendMemberColumnName(MemberExpression node) => LinqStringBuilder.Append(GetColumnNameFromMember(node));

        private void AppendBooleanCondition(MemberExpression node, bool value, bool negate)
        {
            string op = negate ? "<>" : "=";
            LinqStringBuilder.AppendFormat("({0} {1} {2})", GetColumnNameFromMember(node), op, value ? SqlTrue : SqlFalse);
        }

        private static bool IsBooleanPropertyExpression(Expression node)
        {
            if ((node is MemberExpression memberExp) && (memberExp.Member is PropertyInfo propInfo))
            {
                return propInfo.PropertyType == typeof(bool);
            }
            return false;
        }

        private bool CanBeEvaluatedLocally(Expression expression)
        {
            if (expression is ConstantExpression cex)
            {
                if (cex.Value is IQueryable query && query.Provider == this)
                {
                    return false;
                }
            }
            if (expression is MethodCallExpression mc &&
                (mc.Method.DeclaringType == typeof(Enumerable) ||
                 mc.Method.DeclaringType == typeof(Queryable)))
            {
                return false;
            }

            if (expression.NodeType == ExpressionType.Convert &&
                expression.Type == typeof(object))
            {
                return true;
            }

            return expression.NodeType != ExpressionType.Parameter &&
                   expression.NodeType != ExpressionType.Lambda;
        }

        private static Expression ThrowNotSupportedException(MethodCallExpression expression)
            => throw new NotSupportedException(string.Format(Resources.QueryGeneratorMethodNotSupported, expression.Method.Name));

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }

        /// <summary>
        /// Dispatches the expression to one of the more specialized visit methods in this class.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        public override Expression Visit(Expression node)
        {
            if (IsBooleanPropertyExpression(node))
            {
                AppendBooleanCondition((MemberExpression)node, true, false);
                return node;
            }
            if (node is BinaryExpression binExp)
            {
                if (binExp.Left is MethodCallExpression mcExp && IsVbOperatorsExpression(mcExp))
                {
                    return base.Visit(VisitVbOperatorsMethods(mcExp, binExp.NodeType));
                }
            }

            return base.Visit(node);
        }

        private static bool IsVbOperatorsExpression(MethodCallExpression mcExp)
            => (mcExp.Method.DeclaringType.FullName == VbOperatorsClassName) ||
                (mcExp.Method.DeclaringType.FullName == VbEmbeddedOperatorsClassName);

        /// <summary>
        /// Visits the method call.
        /// </summary>
        /// <param name="exp">The method call expression.</param>
        /// <returns>
        /// Reuced expression
        /// </returns>
        protected override Expression VisitMethodCall(MethodCallExpression exp)
        {
            Expression evaluated = PartialEvaluator.Eval(exp, CanBeEvaluatedLocally);

            if (evaluated is ConstantExpression constantExp)
            {
                AddParameterWithValue(GetConstantExpressionValue(constantExp));
                return evaluated;
            }

            if (evaluated is MethodCallExpression methodCallExp)
            {
                if (methodCallExp.Method.DeclaringType == typeof(Queryable))
                {
                    VisitLinqMethods(methodCallExp);

                    return Visit(methodCallExp.Arguments[0]);
                }
                if (methodCallExp.Method.DeclaringType == typeof(string))
                {
                    return VisitStringMethods(methodCallExp);
                }
            }
            return ThrowNotSupportedException(exp);
        }

        /// <summary>
        /// Visit Visual Basic operators expressions.
        /// </summary>
        /// <param name="expression">Method call expression</param>
        /// <param name="binExpNodeType">Binary expression node type</param>
        /// <returns>
        /// Reduced expression.
        /// </returns>
        protected virtual Expression VisitVbOperatorsMethods(MethodCallExpression expression, ExpressionType binExpNodeType)
        {
            if (expression.Method.Name.MatchMethodName("CompareString"))
            {
                return VisitCompare(expression, binExpNodeType);
            }
            return ThrowNotSupportedException(expression);
        }

        /// <summary>
        /// Visit Visual Basic Compare expression
        /// </summary>
        /// <param name="expression">Method call expression Compare expression</param>
        /// <param name="binExpNodeType">Binary expression node type</param>
        /// <returns>
        /// Reduced expression.
        /// </returns>
        protected virtual Expression VisitCompare(MethodCallExpression expression, ExpressionType binExpNodeType)
        {
            var left = expression.Arguments[0];
            var right = expression.Arguments[1];
            switch (binExpNodeType)
            {
                case ExpressionType.Equal:
                    return BinaryExpression.Equal(left, right);

                case ExpressionType.NotEqual:
                    return BinaryExpression.NotEqual(left, right);

                case ExpressionType.LessThan:
                    return BinaryExpression.LessThan(left, right);

                case ExpressionType.LessThanOrEqual:
                    return BinaryExpression.LessThanOrEqual(left, right);

                case ExpressionType.GreaterThan:
                    return BinaryExpression.GreaterThan(left, right);

                case ExpressionType.GreaterThanOrEqual:
                    return BinaryExpression.GreaterThanOrEqual(left, right);
            }

            return ThrowNotSupportedException(expression);
        }

        private void VisitLinqMethods(MethodCallExpression expression)
        {
            SelectExpression = expression.FindSelectExpression();
            LinqStringBuilder = new StringBuilder();

            VisitLinqExpression(expression);
        }

        /// <summary>
        /// Visits the linq expression.
        /// </summary>
        /// <param name="expression">The method call expression.</param>
        /// <returns>
        /// Reduced expression.
        /// </returns>
        protected virtual Expression VisitLinqExpression(MethodCallExpression expression)
        {
            var methodName = expression.Method.Name;

            if (methodName.MatchMethodName(nameof(Enumerable.Where))) return VisitWhere(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.First))) return VisitFirst(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.FirstOrDefault))) return VisitFirst(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.Single))) return VisitFirst(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.SingleOrDefault))) return VisitFirst(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.Skip))) return VisitSkip(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.Take))) return VisitTake(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.GroupBy))) return VisitGroupBy(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.Select))) return VisitSelect(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.OrderBy))) return VisitOrderBy(expression, OrderType.Ascending);
            if (methodName.MatchMethodName(nameof(Enumerable.OrderByDescending))) return VisitOrderBy(expression, OrderType.Descending);
            if (methodName.MatchMethodName(nameof(Enumerable.ThenBy))) return VisitOrderBy(expression, OrderType.Ascending);
            if (methodName.MatchMethodName(nameof(Enumerable.ThenByDescending))) return VisitOrderBy(expression, OrderType.Descending);
            if (methodName.MatchMethodName(nameof(Enumerable.Count))) return VisitCount(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.LongCount))) return VisitLongCount(expression);
            if (methodName.MatchMethodName(nameof(Enumerable.Min))) return VisitAggregate(expression, nameof(Enumerable.Min));
            if (methodName.MatchMethodName(nameof(Enumerable.Max))) return VisitAggregate(expression, nameof(Enumerable.Max));
            if (methodName.MatchMethodName(nameof(Enumerable.Sum))) return VisitAggregate(expression, nameof(Enumerable.Sum));
            if (methodName.MatchMethodName(nameof(Enumerable.Any))) return VisitAny(expression);

            return ThrowNotSupportedException(expression);
        }

        private bool _wasAny;

        /// <summary>
        /// Visits the Linq Any method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        protected virtual Expression VisitAny(MethodCallExpression expression)
        {
            if (expression.Arguments.Count > 1)
            {
                VisitWhere(expression);
            }
            SelectExpression.SetColumnsExpression(new ColumnsExpression("''"));
            _wasAny = true;

            return expression;
        }

        /// <summary>
        /// Visits the Linq <c>Skip</c> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The <paramref name="expression"/> itself.</returns>
        protected virtual Expression VisitSkip(MethodCallExpression expression)
        {
            Visit(StripQuotes(expression.Arguments[1]));
            LinqStringBuilder.Clear();

            try
            {
                _skip = (int)LinqParameters.GetParams().First();

                LinqParameters = new Parameters();
                return expression;
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(Resources.ThisCallOfSkipMethodIsNotSupported, ex);
            }
        }

        /// <summary>
        /// Visits the Linq Take method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <exception cref="System.NotSupportedException">If call of Take method is not supported.</exception>
        protected virtual Expression VisitTake(MethodCallExpression expression)
        {
            Visit(StripQuotes(expression.Arguments[1]));
            LinqStringBuilder.Clear();

            try
            {
                _top = (int)LinqParameters.GetParams().First();

                LinqParameters = new Parameters();
                return expression;
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(Resources.ThisCallOfTakeMethodIsNotSupported, ex);
            }
        }

        /// <summary>
        /// Visits the Linq aggregate methods.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="aggregateName">Name of aggreage method.</param>
        protected virtual Expression VisitAggregate(MethodCallExpression expression, string aggregateName)
        {
            LinqStringBuilder.Append($"{aggregateName.ToUpper()}(");
            Visit(StripQuotes(expression.Arguments[1]));
            LinqStringBuilder.Append(')');

            SelectExpression.SetColumnsExpression(new ColumnsExpression(LinqStringBuilder.ToString()));
            LinqStringBuilder.Clear();

            return expression;
        }

        /// <summary>
        /// Visits the Linq Count method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression VisitCount(MethodCallExpression expression)
        {
            if (expression.Arguments.Count > 1)
            {
                VisitWhere(expression);
            }
            SelectExpression.SetColumnsExpression(new ColumnsExpression("COUNT(*)"));

            return expression;
        }

        /// <summary>
        /// Visits the Linq LongCount method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression VisitLongCount(MethodCallExpression expression)
        {
            if (expression.Arguments.Count > 1)
            {
                VisitWhere(expression);
            }
            SelectExpression.SetColumnsExpression(new ColumnsExpression("COUNT_BIG(*)"));

            return expression;
        }

        /// <summary>
        /// Order type.
        /// </summary>
        protected enum OrderType
        {
            /// <summary>
            /// The ascending.
            /// </summary>
            Ascending,
            /// <summary>
            /// The descending.
            /// </summary>
            Descending
        }

        private Expression VisitOrderBy(MethodCallExpression expression, OrderType orderType)
        {
            var lambda = (LambdaExpression)StripQuotes(expression.Arguments[1]);

            var ret = Visit(lambda);
            Orders.Add(LinqStringBuilder.ToString() + " " + (orderType == OrderType.Ascending ? "ASC" : "DESC"));
            LinqStringBuilder.Clear();

            return ret;
        }

        private static Expression VisitSelect(MethodCallExpression expression)
            => ThrowNotSupportedException(expression);

        private static Expression VisitGroupBy(MethodCallExpression expression)
            => ThrowNotSupportedException(expression);

        /// <summary>
        /// Visits the Linq Where method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression VisitWhere(MethodCallExpression expression)
        {
            LambdaExpression lambda = (LambdaExpression)StripQuotes(expression.Arguments[1]);
            Visit(lambda.Body);

            SelectExpression.SetWhereExpression(new WhereExpression(LinqStringBuilder.ToString(), LinqParameters.GetParams()));
            LinqParameters.Clear();
            LinqStringBuilder = null;

            return expression;
        }

        /// <summary>
        /// Visits the Linq First.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression VisitFirst(MethodCallExpression expression)
        {
            if (expression.Arguments.Count > 1)
            {
                VisitWhere(expression);
            }
            _top = 1;

            return expression;
        }

        /// <summary>
        /// Visits the unary.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">If this unary expression is not supported.</exception>
        protected override Expression VisitUnary(UnaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    if (IsBooleanPropertyExpression(expression.Operand))
                    {
                        AppendBooleanCondition((MemberExpression)expression.Operand, true, true);
                    }
                    else
                    {
                        LinqStringBuilder.Append(" NOT ");
                        Visit(expression.Operand);
                    }
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    Visit(expression.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format(Resources.UnaryOperatorIsNotSupported, expression.NodeType));
            }

            return expression;
        }

        /// <summary>
        /// Visits the Linq Binary.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">If this binary expression is not supported.</exception>
        protected override Expression VisitBinary(BinaryExpression expression)
        {
            bool hasBooleanOperator = HasBooleanOperator(expression);
            LinqStringBuilder.Append('(');
            ProcessBinaryExpressionOperand(expression.Left, hasBooleanOperator);
            AppendOperator(expression);
            ProcessBinaryExpressionOperand(expression.Right, hasBooleanOperator);
            LinqStringBuilder.Append(')');
            return expression;
        }

        private void ProcessBinaryExpressionOperand(Expression operand, bool hasBooleanOperator)
        {
            if (IsBooleanPropertyExpression(operand))
            {
                if (hasBooleanOperator)
                {
                    AppendBooleanCondition((MemberExpression)operand, true, false);
                }
                else
                {
                    AppendMemberColumnName((MemberExpression)operand);
                }
            }
            else
            {
                Visit(operand);
            }
        }

        /// <summary>
        /// Gets the operator.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual string GetOperator(BinaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Equal:
                    return IsCompareToNull(expression) ? "IS" : "=";
                case ExpressionType.NotEqual:
                    return IsCompareToNull(expression) ? "IS NOT" : "<>";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.ExclusiveOr:
                    return "^";
                default:
                    return string.Empty;
            }

            bool IsCompareToNull(BinaryExpression exp)
                => IsNullable(exp) && (exp.Right is ConstantExpression constExpr) && constExpr.Value == null;

            bool IsNullable(BinaryExpression exp)
                => Nullable.GetUnderlyingType(exp.Left.Type) != null;
        }

        private void AppendOperator(BinaryExpression expression)
        {
            string op = GetOperator(expression);
            if (string.IsNullOrEmpty(op))
            {
                throw new NotSupportedException(string.Format(Resources.BinaryOperatorIsNotSupported, expression.NodeType));
            }
            else
            {
                LinqStringBuilder.Append($" {op} ");
            }
        }

        private static bool HasBooleanOperator(BinaryExpression node)
            => (node.NodeType == ExpressionType.And)
                || (node.NodeType == ExpressionType.AndAlso)
                || (node.NodeType == ExpressionType.Or)
                || (node.NodeType == ExpressionType.OrElse);

        /// <summary>
        /// Visits the constant.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <exception cref="NotSupportedException">If type of constant is <see cref="object"/>.</exception>
        protected override Expression VisitConstant(ConstantExpression expression)
        {
            if (expression.Value == null)
            {
                LinqStringBuilder.Append("NULL");
            }
            else if (expression.Value.GetType() == typeof(bool))
            {
                LinqStringBuilder.Append((bool)expression.Value ? SqlTrue : SqlFalse);
            }
            else
            {
                AddParameterWithValue(GetConstantExpressionValue(expression));
            }
            return expression;
        }

        /// <summary>
        /// Returns value of constant expression <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <returns>Value of the <paramref name="expression"/>.</returns>
        protected object GetConstantExpressionValue(ConstantExpression expression)
        {
            object val = expression.Value;
            MemberExpression prevNode;
            while ((prevNode = PreviousMemberExpression) != null)
            {
                if (prevNode.Member is FieldInfo fi)
                {
                    val = fi.GetValue(val);
                }
                else if (prevNode.Member is PropertyInfo pi)
                {
                    val = pi.GetValue(val);
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(Resources.Error_CannotGetValueOfConstantExpression, expression));
                }
                MemberExpressionStack.Pop();
            }
            return val;
        }

        /// <summary>
        /// Visits the member.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <exception cref="System.NotSupportedException">If the member type is not supported.</exception>
        protected override Expression VisitMember(MemberExpression expression)
        {
            if (expression.Expression != null &&
                (expression.Expression.NodeType == ExpressionType.Parameter ||
                 expression.Expression.NodeType == ExpressionType.Convert))
            {
                AppendMemberColumnName(expression);
                return expression;
            }

            MemberExpressionStack.Push(expression);
            Expression result = base.VisitMember(expression);
            if (PreviousMemberExpression == expression)
            {
                MemberExpressionStack.Pop();
            }
            return result;
        }

        /// <summary>
        /// Visits the string methods.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <exception cref="NotSupportedException">If this <see cref="string"/> method is not supported.</exception>
        protected virtual Expression VisitStringMethods(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "StartsWith":
                    return BindStartWith(expression);
                case "EndsWith":
                    return BindEndWith(expression);
                case "Contains":
                    return BindContains(expression);
                case "IsNullOrEmpty":
                    return BindIsNullOrEmpty(expression);
                case "ToUpper":
                    return BindToUpper(expression);
                case "ToLower":
                    return BindToLower(expression);
                case "Replace":
                    return BindReplace(expression);
                case "Substring":
                    return BindSubstring(expression);
                case "Trim":
                    return BindTrim(expression);
                case "Compare":
                    return BindCompare(expression);
            }

            throw new NotSupportedException(string.Format(Resources.MethodIsNotSupported, expression.Method.Name));
        }

        /// <summary>
        /// Binds the <see cref="string.Trim()"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindTrim(MethodCallExpression expression)
        {
            LinqStringBuilder.Append("RTRIM(LTRIM(");
            Visit(expression.Object);
            LinqStringBuilder.Append("))");
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.Substring(int)" autoUpgrade="true" /> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindSubstring(MethodCallExpression expression)
        {
            const int substringMaxLength = 8000;

            LinqStringBuilder.Append("SUBSTRING(");
            Visit(expression.Object);
            LinqStringBuilder.Append(", ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" + 1, ");
            if (expression.Arguments.Count == 2)
            {
                Visit(expression.Arguments[1]);
            }
            else
            {
                AddParameterWithValue(substringMaxLength);
            }
            LinqStringBuilder.Append(')');
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.Replace(string, string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindReplace(MethodCallExpression expression)
        {
            LinqStringBuilder.Append("REPLACE(");
            Visit(expression.Object);
            LinqStringBuilder.Append(", ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(", ");
            Visit(expression.Arguments[1]);
            LinqStringBuilder.Append(')');
            return expression;
        }

        /// <summary>
        /// Binds to <see cref="string.ToLower()"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindToLower(MethodCallExpression expression)
        {
            LinqStringBuilder.Append("LOWER(");
            Visit(expression.Object);
            LinqStringBuilder.Append(')');
            return expression;
        }

        /// <summary>
        /// Binds to <see cref="string.ToUpper()"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindToUpper(MethodCallExpression expression)
        {
            LinqStringBuilder.Append("UPPER(");
            Visit(expression.Object);
            LinqStringBuilder.Append(')');
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.IsNullOrEmpty(string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindIsNullOrEmpty(MethodCallExpression expression)
        {
            LinqStringBuilder.Append('(');
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" IS NULL OR ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" = '')");
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.Contains(string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindContains(MethodCallExpression expression)
        {
            LinqStringBuilder.Append('(');
            Visit(expression.Object);
            LinqStringBuilder.Append(" LIKE '%' + ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" + '%')");
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.EndsWith(string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindEndWith(MethodCallExpression expression)
        {
            LinqStringBuilder.Append('(');
            Visit(expression.Object);
            LinqStringBuilder.Append(" LIKE '%' + ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(')');
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.StartsWith(string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindStartWith(MethodCallExpression expression)
        {
            LinqStringBuilder.Append('(');
            Visit(expression.Object);
            LinqStringBuilder.Append(" LIKE ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" + '%')");
            return expression;
        }

        /// <summary>
        /// Binds the <see cref="string.Compare(string, string)"/> method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected virtual Expression BindCompare(MethodCallExpression expression)
        {
            LinqStringBuilder.Append("CASE WHEN ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" = ");
            Visit(expression.Arguments[1]);
            LinqStringBuilder.Append(" THEN 0 WHEN ");
            Visit(expression.Arguments[0]);
            LinqStringBuilder.Append(" < ");
            Visit(expression.Arguments[1]);
            LinqStringBuilder.Append(" THEN -1 ELSE 1 END");
            return expression;
        }

        /// <summary>
        /// Class which help with Linq query parameters.
        /// </summary>
        protected class Parameters
        {
            private int _parametersCount = 0;
            private readonly List<object> _params = new List<object>();
            private readonly string _parameterNamePrefix;

            /// <summary>
            /// Initializes a new instance of the <see cref="Parameters"/> class.
            /// </summary>
            /// <param name="parameterNamePrefix">The parameter name prefix.</param>
            public Parameters(string parameterNamePrefix = "")
            {
                _parameterNamePrefix = parameterNamePrefix;
            }

            /// <summary>
            /// Gets the name of the next parameter.
            /// </summary>
            public string GetNextParamName() => $"@{_parameterNamePrefix}{++_parametersCount}";

            /// <summary>
            /// Adds the parameter.
            /// </summary>
            /// <param name="param">The parameter.</param>
            public void AddParameter(object param) => _params.Add(param);

            /// <summary>
            /// Gets the parameters.
            /// </summary>
            public object[] GetParams() => _params.ToArray();

            /// <summary>
            /// Clears this instance.
            /// </summary>
            public void Clear() => _params.Clear();
        }

        #endregion
    }

    internal static class StringExtension
    {
        public static bool MatchMethodName(this string methodName, string expected)
            => methodName.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
