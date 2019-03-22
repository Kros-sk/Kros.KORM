using Kros.Utils;
using System;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// Converter, which know convert standard type.
    /// </summary>
    /// <seealso cref="Kros.KORM.Converter.IConverter" />
    /// <example>
    /// For example: convert <code>int</code> to <code>double</code>.
    /// </example>
    internal class TypeConverter:IConverter
    {
        private Type _clrType;
        private Type _dbType;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeConverter"/> class.
        /// </summary>
        /// <param name="clrType">Type of value in clr object.</param>
        /// <param name="dbType">Type of value in db.</param>
        public TypeConverter(Type clrType, Type dbType)
        {
            Check.NotNull(clrType, nameof(clrType));
            Check.NotNull(dbType, nameof(dbType));

            _clrType = clrType;
            _dbType = dbType;
        }

        /// <summary>
        /// Converts the specified value from Db to Clr.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// Converted value for Clr
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Convert(object value)
        {
            return System.Convert.ChangeType(value, _clrType);
        }

        /// <summary>
        /// Converts the value from Clr to Db.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// Converted value for Db
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value)
        {
            return System.Convert.ChangeType(value, _dbType);
        }
    }
}
