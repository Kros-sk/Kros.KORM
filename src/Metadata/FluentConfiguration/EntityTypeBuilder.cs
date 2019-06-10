using Kros.Extensions;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between <typeparamref name="TEntity"/> and database table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class EntityTypeBuilder<TEntity>
        : EntityTypeBuilderBase, IEntityTypeBuilder<TEntity>, IPrimaryKeyBuilder<TEntity> where TEntity : class
    {
        private string _tableName;
        private string _primaryKeyPropertyName;
        private AutoIncrementMethodType _autoIncrementType = AutoIncrementMethodType.None;
        private readonly Dictionary<string, PropertyBuilder<TEntity>> _propertyBuilders
            = new Dictionary<string, PropertyBuilder<TEntity>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Configures the corresponding table name in the database for the <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <returns>
        /// Provider for next fluent entity type definition.
        /// </returns>
        INamedEntityTypeBuilder<TEntity> IEntityTypeBuilder<TEntity>.HasTableName(string tableName)
        {
            _tableName = Check.NotNullOrWhiteSpace(tableName, nameof(tableName));
            return this;
        }

        /// <summary>
        /// Returns an object that can be used to configure primary key.
        /// </summary>
        /// <typeparam name="TProperty">Property type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the primary key property.</param>
        /// <returns>An object that can be used to configure primary key.</returns>
        IPrimaryKeyBuilder<TEntity> INamedEntityTypeBuilder<TEntity>.HasPrimaryKey<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));
            _primaryKeyPropertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);
            return this;
        }

        IEntityPropertyBuilder<TEntity> IPrimaryKeyBuilder<TEntity>.AutoIncrement(AutoIncrementMethodType autoIncrementType)
        {
            _autoIncrementType = autoIncrementType;
            return this;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        IPropertyBuilder<TEntity> IEntityPropertyBuilder<TEntity>.Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));
            string propertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);
            if (_propertyBuilders.ContainsKey(propertyName))
            {
                throw new InvalidOperationException(string.Format("Property \"{0}\" was already configured.", propertyName));
            }

            var propertyBuilder = new PropertyBuilder<TEntity>(this, propertyName);
            _propertyBuilders.Add(propertyName, propertyBuilder);
            return propertyBuilder;
        }

        IPropertyBuilder<TEntity> IPrimaryKeyBuilder<TEntity>.Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => ((IEntityPropertyBuilder<TEntity>)this).Property(propertyExpression);

        internal override void Build(IModelMapperInternal modelMapper)
        {
            if (!_tableName.IsNullOrWhiteSpace())
            {
                modelMapper.SetTableName<TEntity>(_tableName);
            }
            if (!_primaryKeyPropertyName.IsNullOrWhiteSpace())
            {
                modelMapper.SetPrimaryKey<TEntity>(_primaryKeyPropertyName, _autoIncrementType);
            }

            var injectionConfig = new Lazy<InjectionConfiguration<TEntity>>(() => new InjectionConfiguration<TEntity>());
            foreach (PropertyBuilder<TEntity> propertyBuilder in _propertyBuilders.Values)
            {
                if (propertyBuilder.IsMapped)
                {
                    if (!propertyBuilder.ColumnName.IsNullOrWhiteSpace())
                    {
                        modelMapper.SetColumnName<TEntity>(propertyBuilder.PropertyName, propertyBuilder.ColumnName);
                    }
                    if (propertyBuilder.Converter != null)
                    {
                        modelMapper.SetConverter<TEntity>(propertyBuilder.PropertyName, propertyBuilder.Converter);
                    }
                    if (propertyBuilder.Injector != null)
                    {
                        injectionConfig.Value.FillProperty(propertyBuilder.PropertyName, propertyBuilder.Injector);
                    }
                }
                else
                {
                    modelMapper.SetNoMap<TEntity>(propertyBuilder.PropertyName);
                }
            }
            if (injectionConfig.IsValueCreated)
            {
                modelMapper.SetInjector<TEntity>(injectionConfig.Value);
            }
        }
    }
}
