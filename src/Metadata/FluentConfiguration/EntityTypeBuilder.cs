using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    internal class EntityTypeBuilder<TEntity>
        : EntityTypeBuilderBase, IEntityTypeBuilder<TEntity>, IPrimaryKeyBuilder<TEntity> where TEntity : class
    {
        private string _tableName;
        private string _primaryKeyPropertyName;
        private AutoIncrementMethodType _autoIncrementType = AutoIncrementMethodType.None;
        private readonly Dictionary<Type, IConverter> _propertyConverters = new Dictionary<Type, IConverter>();
        private readonly Dictionary<string, PropertyBuilder<TEntity>> _propertyBuilders
            = new Dictionary<string, PropertyBuilder<TEntity>>(StringComparer.OrdinalIgnoreCase);

        INamedEntityTypeBuilder<TEntity> IEntityTypeBuilder<TEntity>.HasTableName(string tableName)
        {
            _tableName = Check.NotNullOrWhiteSpace(tableName, nameof(tableName));
            return this;
        }

        IPrimaryKeyBuilder<TEntity> INamedEntityTypeBuilder<TEntity>.HasPrimaryKey<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));
            _primaryKeyPropertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);
            return this;
        }

        IEntityTypeConvertersBuilder<TEntity> IPrimaryKeyBuilder<TEntity>.AutoIncrement(AutoIncrementMethodType autoIncrementType)
        {
            _autoIncrementType = autoIncrementType;
            return this;
        }

        IEntityTypeConvertersBuilder<TEntity> IEntityTypeConvertersBuilder<TEntity>.UseConverterForProperties<TProperty>(IConverter converter)
        {
            Check.NotNull(converter, nameof(converter));
            Type propertyType = typeof(TProperty);
            if (_propertyConverters.TryGetValue(propertyType, out IConverter currentConverter))
            {
                ThrowHelper.ConverterForTypeAlreadyConfigured<TEntity>(propertyType, converter, currentConverter);
            }
            _propertyConverters.Add(propertyType, converter);
            return this;
        }

        IEntityTypeConvertersBuilder<TEntity> IEntityTypeConvertersBuilder<TEntity>.UseConverterForProperties<TProperty, TConverter>()
            => ((IEntityTypeConvertersBuilder<TEntity>)this).UseConverterForProperties<TProperty>(new TConverter());

        IPropertyBuilder<TEntity> IEntityTypePropertyBuilder<TEntity>.Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));
            string propertyName = PropertyName<TEntity>.GetPropertyName(propertyExpression);
            if (_propertyBuilders.ContainsKey(propertyName))
            {
                ThrowHelper.PropertyAlreadyConfigured<TEntity>(propertyName);
            }

            var propertyBuilder = new PropertyBuilder<TEntity>(this, propertyName);
            _propertyBuilders.Add(propertyName, propertyBuilder);
            return propertyBuilder;
        }

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
            foreach (KeyValuePair<Type, IConverter> item in _propertyConverters)
            {
                modelMapper.SetConverterForProperties<TEntity>(item.Key, item.Value);
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
                    if (propertyBuilder.IgnoreConverter)
                    {
                        modelMapper.SetConverter<TEntity>(propertyBuilder.PropertyName, NoConverter.Instance);
                    }
                    else if (propertyBuilder.Converter != null)
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
