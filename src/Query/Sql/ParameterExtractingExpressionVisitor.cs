using Kros.KORM.Query.Expressions;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Sql
{
    /// <summary>
    /// Class for extracting parameters from Expression.
    /// </summary>
    /// <seealso cref="System.Linq.Expressions.ExpressionVisitor" />
    public class ParameterExtractingExpressionVisitor : ExpressionVisitor
    {
        private DbCommand _command;

        private ParameterExtractingExpressionVisitor(DbCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// Extracts the parrameters to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="expression">The expression.</param>
        public static void ExtractParametersToCommand(DbCommand command, Expression expression)
        {
            Check.NotNull(command, nameof(command));
            Check.NotNull(expression, nameof(expression));

            (new ParameterExtractingExpressionVisitor(command)).Visit(expression);
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
            var expression = node as ArgsExpression;

            if (expression != null)
            {
                VisitArgs(expression);
                return node;
            }
            else if (node != null)
            {
                return base.Visit(node.Reduce());
            }
            else
            {
                return node;
            }
        }

        private void VisitArgs(ArgsExpression argsExpression)
        {
            if (argsExpression.Parameters?.Count() > 0)
            {
                var parameters = new ParamEnumerator(argsExpression.Sql);
                var paramsValuesEnumerator = argsExpression.Parameters.GetEnumerator();

                while (parameters.MoveNext())
                {
                    if (!_command.Parameters.Contains(parameters.Current))
                    {
                        paramsValuesEnumerator.MoveNext();
                        var param = _command.CreateParameter();
                        param.Value = paramsValuesEnumerator.Current ?? DBNull.Value;
                        param.ParameterName = parameters.Current;

                        _command.Parameters.Add(param);
                    }
                }
            }
        }

        private class ParamEnumerator : IEnumerator<string>
        {
            private string _sql;
            private string _current;
            private int _position = 0;
            private const string ParamPrefix = "@";

            public ParamEnumerator(string sql)
            {
                Check.NotNullOrWhiteSpace(sql, nameof(sql));
                _sql = sql.Replace(Environment.NewLine, " ");
                _sql = _sql.Replace("(", " ");
                _sql = _sql.Replace(")", " ");
            }

            public string Current => _current;

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_position < _sql.Length)
                {
                    var start = _sql.IndexOf(ParamPrefix, _position);
                    if (start > -1)
                    {
                        var ends = new List<int>() {
                            _sql.IndexOf(" ", start) ,
                            _sql.IndexOf(",", start),
                            _sql.IndexOf(")", start)}.Where(p => p > -1);

                        var end = _sql.Length;

                        if (ends.Any())
                        {
                            end = ends.Min();
                        }

                        _current = _sql.Substring(start, end - start).TrimStart().TrimEnd();
                        _position = end;

                        return true;
                    }

                    return false;
                }

                return false;
            }

            public void Reset()
            {
                _position = 0;
            }
        }
    }
}
