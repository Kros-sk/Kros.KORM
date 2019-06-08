using Kros.KORM.Properties;
using System;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// <para>
    /// Converter for string values. Based on settings, it can convert <see langword="null"/> and <see cref="DBNull.Value"/>
    /// values to empty string and trim string value.
    /// </para>
    /// <para>
    /// The converter cannot be directly instantiated. Instead of that, there are predefined static instances
    /// <see cref="ConvertNull"/>, <see cref="TrimString"/> and <see cref="ConvertNullAndTrimString"/>.
    /// </para>
    /// <para>
    /// Converter can be used for example to ensure, that value going into the database will be empty string if the column
    /// is set <c>NOT NULL</c>.
    /// </para>
    /// </summary>
    public class NullAndTrimStringConverter : IConverter
    {
        /// <summary>
        /// Instance of the converter with settings:
        /// <see cref="ConvertNullValue"/> is <see langword="true"/>, <see cref="TrimStringValue"/> is <see langword="false"/>.
        /// </summary>
        public static readonly NullAndTrimStringConverter ConvertNull = new NullAndTrimStringConverter(true, false);

        /// <summary>
        /// Instance of the converter with settings:
        /// <see cref="ConvertNullValue"/> is <see langword="false"/>, <see cref="TrimStringValue"/> is <see langword="true"/>.
        /// </summary>
        public static readonly NullAndTrimStringConverter TrimString = new NullAndTrimStringConverter(false, true);

        /// <summary>
        /// Instance of the converter with settings:
        /// <see cref="ConvertNullValue"/> is <see langword="true"/>, <see cref="TrimStringValue"/> is <see langword="true"/>.
        /// </summary>
        public static readonly NullAndTrimStringConverter ConvertNullAndTrimString = new NullAndTrimStringConverter(true, true);

        internal NullAndTrimStringConverter(bool convertNullValue, bool trimStringValue)
        {
            if ((convertNullValue == false) && (trimStringValue == false))
            {
                string msg = Resources.NullAndTrimStringConverter_InvalidArguments;
                throw new ArgumentException(
                    string.Format(msg, nameof(convertNullValue), nameof(trimStringValue)),
                    nameof(convertNullValue) + ", " + nameof(trimStringValue));
            }
            ConvertNullValue = convertNullValue;
            TrimStringValue = trimStringValue;
        }

        /// <summary>
        /// If <see langword="true"/>, the values <see langword="null"/> and <see cref="DBNull"/> for database are converted
        /// to empty string.
        /// </summary>
        public bool ConvertNullValue { get; }

        /// <summary>
        /// If <see langword="true"/> and input value for database is string, it is trimmed
        /// (<see cref="string.Trim()">String.Trim</see>).
        /// </summary>
        public bool TrimStringValue { get; }

        /// <inheritdoc cref="ConvertBack(object)"/>
        public object Convert(object value) => ConvertValue(value);

        /// <summary>
        /// Converts <paramref name="value"/> based on <see cref="ConvertNullValue"/> and <see cref="TrimStringValue"/> settings.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>If <see cref="ConvertNullValue"/> is <see langword="true"/> and the <paramref name="value"/> is
        /// <see langword="null"/> or <see cref="DBNull"/>, empty string is returned.</item>
        /// <item>If <paramref name="value"/> is string and <see cref="TrimStringValue"/> is <see langword="true"/>,
        /// input value is trimmed and returned (<see cref="string.Trim()">String.Trim</see>).</item>
        /// <item>In all other cases the <paramref name="value"/> is returned unchanged.</item>
        /// </list>
        /// </returns>
        public object ConvertBack(object value) => ConvertValue(value);

        private object ConvertValue(object value)
        {
            if ((value is null) || (value == DBNull.Value))
            {
                return ConvertNullValue ? string.Empty : value;
            }
            if (value is string strValue)
            {
                return TrimStringValue ? strValue.Trim() : strValue;
            }
            return value;
        }
    }
}
