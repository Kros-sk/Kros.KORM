using System;
using System.Linq.Expressions;

namespace Kros.KORM.Helper
{
    /// <summary>
    /// Helper for getting property name from class.
    /// </summary>
    /// <typeparam name="P">Class type, from want get property name</typeparam>
    /// <example>
    ///   <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\PropertyNameExample.cs" title="Get property name" region="GetPropertyName" language="cs" />
    /// </example>
    public static class PropertyName<P> where P : class
    {
        #region Methods

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">The expression with property.</param>
        /// <returns>
        /// Property name.
        /// </returns>
        /// <example>
        ///   <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\PropertyNameExample.cs" title="Get property name" region="GetPropertyName" language="cs" />
        /// </example>
        public static string GetPropertyName<T>(Expression<Func<P, T>> expression)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            var propertyName = memberExpression.Member.Name;

            return propertyName;
        }

        #endregion
    }
}
