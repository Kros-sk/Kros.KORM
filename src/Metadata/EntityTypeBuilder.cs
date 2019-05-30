using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between <typeparamref name="TEntity"/> and database table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class EntityTypeBuilder<TEntity> where TEntity : class
    {
        /// <summary>
        /// Configures the corresponding table name in the database for the <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <returns>
        /// Provider for next fluent entity type definition.
        /// </returns>
        public EntityTypeBuilder<TEntity> HasTableName(string tableName) => this;

        /// <summary>
        /// Returns an object that can be used to configure primary key.
        /// </summary>
        /// <typeparam name="TProperty">Property type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the primary key property.</param>
        /// <returns>An object that can be used to configure primary key.</returns>
        public virtual PrimaryKeyBuilder<TEntity> HasPrimaryKey<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression) => null;

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
