using Kros.KORM.Helper;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provide a simple fluent API for building mapping definition between <typeparamref name="TEntity"/> and database table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class EntityTypeBuilder<TEntity> where TEntity : class
    {
        private string _tableName;
        private PrimaryKeyBuilder<TEntity> _primaryKeyBuilder;
        private readonly Dictionary<string, PropertyBuilder<TEntity>> _propertyBuilders
            = new Dictionary<string, PropertyBuilder<TEntity>>();

        /// <summary>
        /// Configures the corresponding table name in the database for the <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <returns>
        /// Provider for next fluent entity type definition.
        /// </returns>
        public EntityTypeBuilder<TEntity> HasTableName(string tableName)
        {
            if (_tableName != null)
            {
                throw new InvalidOperationException("Nie nie toto nemôžeš.");
            }

            _tableName = Check.NotNullOrWhiteSpace(tableName, nameof(tableName));

            return this;
        }

        /// <summary>
        /// Returns an object that can be used to configure primary key.
        /// </summary>
        /// <typeparam name="TProperty">Property type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the primary key property.</param>
        /// <returns>An object that can be used to configure primary key.</returns>
        public virtual PrimaryKeyBuilder<TEntity> HasPrimaryKey<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            if (_primaryKeyBuilder != null)
            {
                throw new InvalidOperationException("Nie nie toto nemôžeš.");
            }

            _primaryKeyBuilder = new PrimaryKeyBuilder<TEntity>(this, PropertyName<TEntity>.GetPropertyName(propertyExpression));

            return _primaryKeyBuilder;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            string propertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);

            if (_propertyBuilders.ContainsKey(propertyName))
            {
                throw new InvalidOperationException("Nie nie toto nemôžeš.");
            }

            var propertyBuilder = new PropertyBuilder<TEntity>(this, propertyName);
            _propertyBuilders.Add(propertyName, propertyBuilder);

            return propertyBuilder;
        }
    }
}
