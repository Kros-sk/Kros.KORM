using System;
using Kros.KORM.Metadata;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// Helper, which provides support for converting values.
    /// </summary>
    internal static class ConverterHelper
    {
        private static readonly GuidToStringConverter _guidToStringConverter = new GuidToStringConverter();

        /// <summary>
        /// Gets converter based on column data type.
        /// </summary>
        /// <param name="columnInfo">Information about column.</param>
        /// <param name="dbType">Column data type.</param>
        /// <returns>Converter, which is required for the conversion values.</returns>
        internal static IConverter GetConverter(ColumnInfo columnInfo, Type dbType)
        {
            if (columnInfo.Converter != null)
            {
                return columnInfo.Converter;
            }
            else if (columnInfo.PropertyInfo.PropertyType.IsEnum)
            {
                return new IntToEnumConverter(columnInfo.PropertyInfo.PropertyType);
            }
            else if (columnInfo.PropertyInfo.PropertyType == typeof(string) && dbType == typeof(Guid))
            {
                return _guidToStringConverter;
            }
            else if (columnInfo.PropertyInfo.PropertyType != dbType)
            {
                var type = Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType);
                if (type != null && type == dbType)
                {
                    return null;
                }
                else
                {
                    return new TypeConverter(type != null ? type : columnInfo.PropertyInfo.PropertyType, dbType);
                }
            }

            return null;
        }
    }
}
