using Kros.KORM.Properties;
using System;

namespace Kros.KORM.Metadata
{
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
