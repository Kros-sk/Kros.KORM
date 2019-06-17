using Kros.KORM.Converter;
using Kros.KORM.Properties;
using System;

namespace Kros.KORM
{
    internal static class ThrowHelper
    {
        public static void PropertyAlreadyConfigured(string propertyName)
            => throw new InvalidOperationException(string.Format(Resources.ThrowHelper_PropertyAlreadyConfigured, propertyName));

        public static void ColumnMappingAlreadyConfigured(string propertyName, string columnName, string currentMapping)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ColumnMappingAlreadyConfigured, columnName, propertyName, currentMapping));

        public static void ConverterAlreadyConfigured(string propertyName, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ConverterAlreadyConfigured,
                converter.GetType().FullName, propertyName, currentConverter.GetType().FullName));

        public static void ConverterForTypeAlreadyConfigured(Type propertyType, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format(Resources.ThrowHelper_ConverterForTypeAlreadyConfigured,
                converter.GetType().FullName, propertyType.Name, currentConverter.GetType().FullName));
    }
}
