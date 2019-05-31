using Kros.Utils;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between <typeparamref name="TEntity"/>
    /// property and database table column.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class PropertyBuilder<TEntity> where TEntity : class
    {
        private readonly EntityTypeBuilder<TEntity> _entityTypeBuilder;
        private readonly string _propertyName;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity type builder.</param>
        /// <param name="propertyName">Name of property which represent primary key.</param>
        internal PropertyBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName)
        {
            _entityTypeBuilder = Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            _propertyName = Check.NotNullOrEmpty(propertyName, nameof(propertyName));
        }

        /// <summary>
        /// Configures the corresponding column name in the database for the property.
        /// </summary>
        /// <param name="columnName">Column name.</param>
        public PropertyBuilder<TEntity> HasColumnName(string columnName) => this;

        /// <summary>
        /// Configures that the property should not be mapped to a column.
        /// </summary>
        public PropertyBuilder<TEntity> NoMap() => this;

        /// <summary>
        /// Configure converter for property value.
        /// </summary>
        /// <typeparam name="TConverter">Converter type.</typeparam>
        public PropertyBuilder<TEntity> UseConverter<TConverter>() => this;

        /// <summary>
        /// Configure injector delegate for injecting values to property.
        /// </summary>
        /// <param name="injector">Delegate for injecting values to property.</param>
        public PropertyBuilder<TEntity> InjectValue<TProperty>(Func<TProperty> injector) => this;

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => _entityTypeBuilder.Property(propertyExpression);
    }
}
