using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for definition primary key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class PrimaryKeyBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures the corresponding primary key constraint name in the database.
        /// </summary>
        /// <param name="constraintName">Primary key contstraint name.</param>
        public PrimaryKeyBuilder<TEntity> WithName(string constraintName) => this;

        /// <summary>
        /// Configure autoincrement for primary key.
        /// </summary>
        /// <param name="autoIncrementMethodType">Autoincrement method type.</param>
        public PrimaryKeyBuilder<TEntity> AutoIncrement(
            AutoIncrementMethodType autoIncrementMethodType = AutoIncrementMethodType.Identity)
            => this;

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity, TProperty> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression) => null;
    }
}
