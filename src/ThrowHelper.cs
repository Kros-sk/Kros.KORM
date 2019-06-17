using Kros.KORM.Converter;
using System;

namespace Kros.KORM
{
    internal static class ThrowHelper
    {
        // RES:
        public static void PropertyAlreadyConfigured(string propertyName)
            => throw new InvalidOperationException(string.Format("Property \"{0}\" was already configured.", propertyName));

        public static void ColumnMappingAlreadyConfigured(string propertyName, string columnName, string currentMapping)
            => throw new InvalidOperationException(
                string.Format("Trying to set column mapping \"{0}\" for property \"{1}\". Column mapping is already set as \"{2}\".",
                columnName, propertyName, currentMapping));

        public static void ConverterAlreadyConfigured(string propertyName, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format("Trying to set converter \"{0}\" for property \"{1}\". Converter is already set as \"{2}\".",
                converter, propertyName, currentConverter.GetType().FullName));

        public static void ConverterForTypeAlreadyConfigured(Type propertyType, IConverter converter, IConverter currentConverter)
            => throw new InvalidOperationException(
                string.Format("Trying to set converter \"{0}\" for properties of type \"{1}\". Converter is already set as \"{2}\".",
                converter.GetType().FullName, propertyType.Name, currentConverter.GetType().FullName));
    }
}
