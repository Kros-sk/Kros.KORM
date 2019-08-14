using Kros.KORM.Converter;
using Kros.KORM.Properties;
using Kros.KORM.ValueGeneration;
using Kros.Utils;
using System;
using System.Linq.Expressions;

namespace Kros.KORM.Metadata
{
    internal class PropertyBuilder<TEntity>
        : IPropertyBuilder<TEntity>, INamedPropertyBuilder<TEntity> where TEntity : class
    {
        private readonly IEntityTypePropertyBuilder<TEntity> _entityTypeBuilder;
        private bool _isMapped = true;
        private string _columnName;
        private IConverter _converter;
        private bool _ignoreConverter = false;
        private ValueGenerated _valueGenerated;
        private IValueGenerator _valueGenerator;
        private Func<object> _injector;

        internal PropertyBuilder(IEntityTypePropertyBuilder<TEntity> entityTypeBuilder, string propertyName)
        {
            _entityTypeBuilder = Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
            PropertyName = Check.NotNullOrEmpty(propertyName, nameof(propertyName));
        }

        internal string PropertyName { get; }
        internal bool IsMapped => _isMapped;
        internal string ColumnName => _columnName;
        internal IConverter Converter => _converter;
        internal bool IgnoreConverter => _ignoreConverter;
        internal ValueGenerated ValueGenerated => _valueGenerated;
        internal IValueGenerator ValueGenerator => _valueGenerator;
        internal Func<object> Injector => _injector;

        IEntityTypePropertyBuilder<TEntity> IPropertyBuilder<TEntity>.NoMap()
        {
            _isMapped = false;
            return _entityTypeBuilder;
        }

        INamedPropertyBuilder<TEntity> IPropertyBuilder<TEntity>.HasColumnName(string columnName)
        {
            _columnName = Check.NotNullOrWhiteSpace(columnName, nameof(columnName));
            return this;
        }

        IPropertyBuilder<TEntity> INamedPropertyBuilder<TEntity>.Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression)
            => _entityTypeBuilder.Property(propertyExpression);

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseConverter<TConverter>()
            => ((IMappedPropertyBuilder<TEntity>)this).UseConverter(new TConverter());

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseConverter(IConverter converter)
        {
            _converter = Check.NotNull(converter, nameof(converter));
            return _entityTypeBuilder;
        }

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.IgnoreConverter()
        {
            _ignoreConverter = true;
            return _entityTypeBuilder;
        }

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnInsert<TValueGenerator>()
            => ((IMappedPropertyBuilder<TEntity>)this).UseValueGeneratorOnInsert(new TValueGenerator());

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnInsert(IValueGenerator generator)
        {
            SetValueGeneratorAndValueGenerated(generator, ValueGenerated.OnInsert);
            return _entityTypeBuilder;
        }

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnUpdate<TValueGenerator>()
            => ((IMappedPropertyBuilder<TEntity>)this).UseValueGeneratorOnUpdate(new TValueGenerator());

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnUpdate(IValueGenerator generator)
        {
            SetValueGeneratorAndValueGenerated(generator, ValueGenerated.OnUpdate);
            return _entityTypeBuilder;
        }

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnInsertOrUpdate<TValueGenerator>()
            => ((IMappedPropertyBuilder<TEntity>)this).UseValueGeneratorOnInsertOrUpdate(new TValueGenerator());

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.UseValueGeneratorOnInsertOrUpdate(IValueGenerator generator)
        {
            SetValueGeneratorAndValueGenerated(generator, ValueGenerated.OnInsertOrUpdate);
            return _entityTypeBuilder;
        }

        private void SetValueGeneratorAndValueGenerated(IValueGenerator generator, ValueGenerated valueGenerated)
        {
            _valueGenerator = Check.NotNull(generator, nameof(generator));
            _valueGenerated = valueGenerated;
        }

        IEntityTypePropertyBuilder<TEntity> IMappedPropertyBuilder<TEntity>.InjectValue(Func<object> injector)
        {
            _injector = Check.NotNull(injector, nameof(injector));
            return _entityTypeBuilder;
        }
    }

    internal static class PropertyBuilderExtensions
    {
        internal static IEntityTypePropertyBuilder<TEntity> UseCurrentTimeValueGenerator<TEntity>(
            this IPropertyBuilder<TEntity> propertyBuilder, ValueGenerated valueGenerated) where TEntity : class
        {
            switch (valueGenerated)
            {
                case ValueGenerated.OnInsert:
                    return propertyBuilder.UseValueGeneratorOnInsert(new CurrentTimeValueGenerator());

                case ValueGenerated.OnUpdate:
                    return propertyBuilder.UseValueGeneratorOnUpdate(new CurrentTimeValueGenerator());

                case ValueGenerated.OnInsertOrUpdate:
                    return propertyBuilder.UseValueGeneratorOnInsertOrUpdate(new CurrentTimeValueGenerator());
            }

            throw new NotSupportedException(
                string.Format(Resources.ValueGeneratedNeverNotSupported, nameof(CurrentTimeValueGenerator)));
        }
    }
}
