using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kros.KORM.Helper
{
    /// <summary>
    /// Helper for getting method name.
    /// </summary>
    /// <typeparam name="T">Type of class or interface, which method we want.</typeparam>
    public static class MethodName<T>
    {
        /// <summary>
        /// Gets the name of method.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// Method name.
        /// </returns>
        public static string GetName(Expression<Action<T>> expression)
        {
            MethodCallExpression memberExpression = (MethodCallExpression) expression.Body;

            return memberExpression.Method.Name;
        }
    }
}
