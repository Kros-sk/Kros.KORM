using Kros.KORM.Query.Expressions;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// Interface, which describe visitor for genereting sql select command.
    /// </summary>
    public interface ISqlExpressionVisitor
    {
        /// <summary>
        /// Generates the SQL from expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>SQL select command text.</returns>
        QueryInfo GenerateSql(Expression expression);

        /// <summary>
        /// Generate the SQL WHERE condition from <paramref name="whereExpression"/>.
        /// </summary>
        /// <param name="whereExpression">Where condition written by LINQ expression.</param>
        /// <param name="parameterNamePrefix">Prefix of parameter name.</param>
        /// <returns>
        /// <see cref="WhereExpression"/> which contains generated SQL WHERE condition and parameters.
        /// </returns>
        /// <remarks>
        /// SQL WHERE condition is generated without WHERE keyword.
        /// </remarks>
        WhereExpression GenerateWhereCondition(Expression whereExpression, string parameterNamePrefix = "");

        /// <summary>
        /// Visits the SQL.
        /// </summary>
        /// <param name="sqlExpression">The SQL expression.</param>
        /// <returns>Expression</returns>
        Expression VisitSql(SqlExpression sqlExpression);

        /// <summary>
        /// Visits the select.
        /// </summary>
        /// <param name="selectExpression">The select expression.</param>
        /// <returns>Expression</returns>
        Expression VisitSelect(SelectExpression selectExpression);

        /// <summary>
        /// Visits the columns.
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns>Expression</returns>
        Expression VisitColumns(ColumnsExpression columnExpression);

        /// <summary>
        /// Visits the table.
        /// </summary>
        /// <param name="tableExpression">The table expression.</param>
        /// <returns>Expression</returns>
        Expression VisitTable(TableExpression tableExpression);

        /// <summary>
        /// Visits the where.
        /// </summary>
        /// <param name="whereExpression">The where expression.</param>
        /// <returns>Expression</returns>
        Expression VisitWhere(WhereExpression whereExpression);

        /// <summary>
        /// Visits the group by.
        /// </summary>
        /// <param name="groupByExpression">The group by expression.</param>
        /// <returns>Expression</returns>
        Expression VisitGroupBy(GroupByExpression groupByExpression);

        /// <summary>
        /// Visits the order by.
        /// </summary>
        /// <param name="orderByExpression">The order by expression.</param>
        /// <returns>Expression</returns>
        Expression VisitOrderBy(OrderByExpression orderByExpression);
    }
}
