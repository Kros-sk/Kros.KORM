﻿using Kros.KORM.Query.Expressions;
using Kros.Utils;
using System;
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
    }
}
