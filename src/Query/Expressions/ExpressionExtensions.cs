using System;
using System.Linq.Expressions;

namespace Kros.KORM.Query.Expressions
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Find <see cref="SelectExpression"/> in expression argument.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <returns>
        /// <see cref="SelectExpression"/> if exist.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If expression in arguments is not type of <see cref="MethodCallExpression"/>.
        /// </exception>
        public static SelectExpression FindSelectExpression(this MethodCallExpression expression)
        {
            if (expression.Arguments[0] is SelectExpression selectExpression)
            {
                return selectExpression;
            }
            else if (expression.Arguments[0] is MethodCallExpression methodCallExpression)
            {
                return FindSelectExpression(methodCallExpression);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
