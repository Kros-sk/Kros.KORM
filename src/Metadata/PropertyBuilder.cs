using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between <typeparamref name="TEntity"/>
    /// property and database table column.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    public class PropertyBuilder<TEntity, TProperty> where TEntity : class
    {
        /// <summary>
        /// Configures the corresponding column name in the database for the property.
        /// </summary>
        /// <param name="columnName">Column name.</param>
        public PropertyBuilder<TEntity, TProperty> HasColumnName(string columnName) => this;

        /// <summary>
        /// Configures that the property should not be mapped to a column.
        /// </summary>
        public PropertyBuilder<TEntity, TProperty> NoMap() => this;

        /// <summary>
        /// Configure converter for property value.
        /// </summary>
        /// <typeparam name="TConverter">Converter type.</typeparam>
        public PropertyBuilder<TEntity, TProperty> UseConverter<TConverter>() => this;

        /// <summary>
        /// Configure injector delegate for injecting values to property.
        /// </summary>
        /// <param name="injector">Delegate for injecting values to property.</param>
        public PropertyBuilder<TEntity, TProperty> InjectValue(Func<TProperty> injector) => this;

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TNextProperty">Propery type</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity, TNextProperty> Property<TNextProperty>(
            Expression<Func<TEntity, TNextProperty>> propertyExpression) => null;
    }
}
