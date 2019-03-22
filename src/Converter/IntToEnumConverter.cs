using Kros.KORM.Properties;
using Kros.Utils;
using System;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// Converter, which know convert int from Db to enum value
    /// </summary>
    /// <seealso cref="Kros.KORM.Converter.IConverter" />
    internal class IntToEnumConverter : IConverter
    {
        private readonly Type _enumType;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntToEnumConverter"/> class.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <exception cref="ArgumentNullException">The value of <paramref name="enumType"/> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="enumType"/> is not an <c>Enum</c> type.</exception>
        public IntToEnumConverter(Type enumType)
        {
            Check.NotNull(enumType, nameof(enumType));
            if (!enumType.IsEnum)
            {
                throw new ArgumentException(Resources.TypeMustBeEnumType, nameof(enumType));
            }

            _enumType = enumType;
        }

        /// <summary>
        /// Converts the specified int value from Db to Clr enum value.
        /// </summary>
        /// <param name="value">The int value.</param>
        /// <returns>
        /// Converted enum value for Clr.
        /// </returns>
        public object Convert(object value)
        {
            return Enum.ToObject(_enumType, value);
        }

        /// <summary>
        /// Converts the enum value from Clr to Db int value.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>
        /// Converted int value for Db
        /// </returns>
        public object ConvertBack(object value)
        {
            return (int)value;
        }
    }
}
