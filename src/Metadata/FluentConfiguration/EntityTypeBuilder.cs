using Kros.Extensions;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Metadata.FluentConfiguration;
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
    public class EntityTypeBuilder<TEntity> : EntityTypeBuilderBase where TEntity : class
    {
        private string _tableName;
        private PrimaryKeyBuilder<TEntity> _primaryKeyBuilder;
        private readonly Dictionary<string, PropertyBuilder<TEntity>> _propertyBuilders
            = new Dictionary<string, PropertyBuilder<TEntity>>();
        private Lazy<InjectionConfiguration<TEntity>> _injector =
            new Lazy<InjectionConfiguration<TEntity>>(() => new InjectionConfiguration<TEntity>());

        /// <summary>
        /// Configures the corresponding table name in the database for the <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <returns>
        /// Provider for next fluent entity type definition.
        /// </returns>
        public EntityTypeBuilder<TEntity> HasTableName(string tableName)
        {
            ExceptionHelper.CheckMultipleTimeCalls(() => _tableName != null);

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
            ExceptionHelper.CheckMultipleTimeCalls(() => _primaryKeyBuilder != null);

            _primaryKeyBuilder = new PrimaryKeyBuilder<TEntity>(this, PropertyName<TEntity>.GetPropertyName(propertyExpression));

            return _primaryKeyBuilder;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            string propertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);
            PropertyBuilder<TEntity> propertyBuilder;

            if (_propertyBuilders.ContainsKey(propertyName))
            {
                propertyBuilder = _propertyBuilders[propertyName];
            }
            else
            {
                propertyBuilder = new PropertyBuilder<TEntity>(this, propertyName);
                _propertyBuilders.Add(propertyName, propertyBuilder);
            }

            return propertyBuilder;
        }

        internal InjectionConfiguration<TEntity> Injector => _injector.Value;

        internal override void Build(IModelMapperInternal modelMapper)
        {
            if (!_tableName.IsNullOrWhiteSpace())
            {
                modelMapper.SetTableName<TEntity>(_tableName);
            }
            if (_primaryKeyBuilder != null)
            {
                _primaryKeyBuilder.Build(modelMapper);
            }
            if (_injector.IsValueCreated)
            {
                modelMapper.SetInjector<TEntity>(_injector.Value);
            }

            foreach (var propertyBuilder in _propertyBuilders.Values)
            {
                propertyBuilder.Build(modelMapper);
            }
        }
    }
}
