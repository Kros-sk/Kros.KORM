using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kros.KORM.Injection
{
    /// <summary>
    /// Configurator, for configurate model property injection.
    /// </summary>
    /// <typeparam name="TModel">Model type.</typeparam>
    internal class InjectionConfiguration<TModel> : IInjectionConfigurator<TModel>, IInjector
    {
        #region Private fields

        private Dictionary<string, Func<object>> _injectors =
            new Dictionary<string, Func<object>>(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        /// <summary>
        /// Fill model property with injector.
        /// </summary>
        /// <typeparam name="TValue">Property type.</typeparam>
        /// <param name="modelProperty">Expression for defined property for injection.</param>
        /// <param name="injector">Function which is used for injection value to property.</param>
        /// <returns>Configurator, for next configurations.</returns>
        public IInjectionConfigurator<TModel> FillProperty<TValue>(
            Expression<Func<TModel, TValue>> modelProperty,
            Func<TValue> injector)
        {
            MemberExpression memberExpression = (MemberExpression)modelProperty.Body;
            var propertyName = memberExpression.Member.Name;

            Func<object> function = () => injector() as object;
            _injectors[propertyName] = function;

            return this;
        }

        /// <summary>
        /// Get injected value for property.
        /// </summary>
        /// <param name="propertyName">Property, which want resolve.</param>
        /// <returns>
        /// Value for injection.
        /// </returns>
        public object GetValue(string propertyName)
        {
            Check.NotNullOrWhiteSpace(propertyName, nameof(propertyName));

            if (IsInjectable(propertyName))
            {
                return _injectors[propertyName]();
            }
            else
            {
                throw new InvalidOperationException(string.Format(Resources.NoInjectionConfigurationForProperty, propertyName));
            }
        }

        /// <summary>
        /// Can by property injected?
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <returns><see langword="true"/> if can by injected, otherwise <see langword="false"/>.</returns>
        public bool IsInjectable(string propertyName) => _injectors.ContainsKey(propertyName);
    }
}
