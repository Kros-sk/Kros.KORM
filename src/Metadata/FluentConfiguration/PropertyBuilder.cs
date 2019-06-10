﻿using Kros.KORM.Converter;
using Kros.Utils;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between <typeparamref name="TEntity"/>
    /// property and database table column.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class PropertyBuilder<TEntity> : IPropertyBuilder<TEntity> where TEntity : class
    {
        private readonly IEntityPropertyBuilder<TEntity> _entityTypeBuilder;
        private bool _isMapped = true;
        private string _columnName;
        private IConverter _converter;
        private Func<object> _injector;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity type builder.</param>
        /// <param name="propertyName">Name of property which represents primary key.</param>
        internal PropertyBuilder(IEntityPropertyBuilder<TEntity> entityTypeBuilder, string propertyName)
        {
            _entityTypeBuilder = Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            PropertyName = Check.NotNullOrEmpty(propertyName, nameof(propertyName));
        }

        internal string PropertyName { get; }
        internal bool IsMapped => _isMapped;
        internal string ColumnName => _columnName;
        internal IConverter Converter => _converter;
        internal Func<object> Injector => _injector;

        /// <summary>
        /// Configures that the property should not be mapped to a column.
        /// </summary>
        IEntityPropertyBuilder<TEntity> IPropertyBuilder<TEntity>.NoMap()
        {
            _isMapped = false;
            return _entityTypeBuilder;
        }

        /// <summary>
        /// Configures the corresponding column name in the database for the property.
        /// </summary>
        /// <param name="columnName">Column name.</param>
        INamedPropertyBuilder<TEntity> IPropertyBuilder<TEntity>.HasColumnName(string columnName)
        {
            _columnName = Check.NotNullOrWhiteSpace(columnName, nameof(columnName));
            return this;
        }

        IPropertyBuilder<TEntity> INamedPropertyBuilder<TEntity>.Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => _entityTypeBuilder.Property(propertyExpression);

        IEntityPropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseConverter<TConverter>()
            => ((IMappedPropertyBuilder<TEntity>)this).UseConverter(new TConverter());

        IEntityPropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseConverter(IConverter converter)
        {
            _converter = Check.NotNull(converter, nameof(converter));
            return _entityTypeBuilder;
        }

        /// <summary>
        /// Configures injector delegate for injecting values to property.
        /// </summary>
        /// <param name="injector">Delegate for injecting values to property.</param>
        IEntityPropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.InjectValue(Func<object> injector)
        {
            _injector = Check.NotNull(injector, nameof(injector));
            return _entityTypeBuilder;
        }
    }
}
