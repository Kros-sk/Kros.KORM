using Kros.KORM.Converter;
using Kros.KORM.Properties;
using System;

namespace Kros.KORM
{
    internal static class ThrowHelper
    {
        public static void PropertyAlreadyConfigured<TEntity>(string propertyName)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_PropertyAlreadyConfigured, propertyName, typeof(TEntity).Name));

        public static void ColumnMappingAlreadyConfigured<TEntity>(string propertyName, string columnName, string currentMapping)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ColumnMappingAlreadyConfigured,
                    columnName, propertyName, typeof(TEntity).Name, currentMapping));

        public static void ConverterAlreadyConfigured<TEntity>(
            string propertyName, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ConverterAlreadyConfigured,
                    converter.GetType().FullName, propertyName, typeof(TEntity).Name, currentConverter.GetType().FullName));

        public static void ConverterForTypeAlreadyConfigured<TEntity>(
            Type propertyType, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ConverterForTypeAlreadyConfigured,
                    converter.GetType().FullName, propertyType.Name, typeof(TEntity).Name, currentConverter.GetType().FullName));
    }
}
