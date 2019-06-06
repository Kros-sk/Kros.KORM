using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata.FluentConfiguration;
using Kros.Utils;
using System;
using System.Linq.Expressions;
using Kros.Extensions;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Provides a simple fluent API for building mapping definition between <typeparamref name="TEntity"/>
    /// property and database table column.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    public class PropertyBuilder<TEntity> where TEntity : class
    {
        private readonly EntityTypeBuilder<TEntity> _entityTypeBuilder;
        private readonly string _propertyName;
        private string _columnName;
        private bool _noMap = false;
        private IConverter _converter;
        private bool _wasInjectorSetup = false;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity type builder.</param>
        /// <param name="propertyName">Name of property which represents primary key.</param>
        internal PropertyBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName)
        {
            _entityTypeBuilder = Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            _propertyName = Check.NotNullOrEmpty(propertyName, nameof(propertyName));
        }

        /// <summary>
        /// Configures the corresponding column name in the database for the property.
        /// </summary>
        /// <param name="columnName">Column name.</param>
        public PropertyBuilder<TEntity> HasColumnName(string columnName)
        {
            ExceptionHelper.CheckMultipleTimeCalls(() => _columnName != null);
            CheckNoMapOrInjector();

            _columnName = Check.NotNullOrWhiteSpace(columnName, nameof(columnName));

            return this;
        }

        /// <summary>
        /// Configures that the property should not be mapped to a column.
        /// </summary>
        public PropertyBuilder<TEntity> NoMap()
        {
            ExceptionHelper.CheckMultipleTimeCalls(() => _noMap);
            _noMap = true;

            return this;
        }

        /// <summary>
        /// Configures converter for property value.
        /// </summary>
        /// <typeparam name="TConverter">Converter type.</typeparam>
        public PropertyBuilder<TEntity> UseConverter<TConverter>() where TConverter : IConverter, new()
            => UseConverter(new TConverter());

        /// <summary>
        /// Configures converter for property value.
        /// </summary>
        /// <param name="converter">Converter instance.</param>
        public PropertyBuilder<TEntity> UseConverter(IConverter converter)
        {
            ExceptionHelper.CheckMultipleTimeCalls(() => _converter != null);
            CheckNoMapOrInjector();

            _converter = Check.NotNull(converter, nameof(converter));

            return this;
        }

        /// <summary>
        /// Configures injector delegate for injecting values to property.
        /// </summary>
        /// <param name="injector">Delegate for injecting values to property.</param>
        public PropertyBuilder<TEntity> InjectValue<TProperty>(Func<TProperty> injector)
        {
            if (!string.IsNullOrEmpty(_columnName) || _converter != null)
            {
                throw new InvalidOperationException(Properties.Resources.CannotCallMethod.Format(nameof(InjectValue)));
            }
            ExceptionHelper.CheckMultipleTimeCalls(() => _wasInjectorSetup);

            _wasInjectorSetup = true;
            _entityTypeBuilder.Injector.FillProperty(_propertyName, injector);

            return this;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of the entity type.
        /// </summary>
        /// <typeparam name="TProperty">Propery type.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured.</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public virtual PropertyBuilder<TEntity> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => _entityTypeBuilder.Property(propertyExpression);

        internal void Build(IModelMapperInternal modelMapper)
        {
            if (!_columnName.IsNullOrWhiteSpace())
            {
                modelMapper.SetColumnName<TEntity>(_propertyName, _columnName);
            }
            if (_noMap)
            {
                modelMapper.SetNoMap<TEntity>(_propertyName);
            }
            if (_converter != null)
            {
                modelMapper.SetConverter<TEntity>(_propertyName, _converter);
            }
        }

        private void CheckNoMapOrInjector()
        {
            if (_noMap || _wasInjectorSetup)
            {
                throw new InvalidOperationException(
                    Properties.Resources.CannotConfigureAnythingElse.Format(nameof(NoMap), nameof(InjectValue)));
            }
        }
    }
}
