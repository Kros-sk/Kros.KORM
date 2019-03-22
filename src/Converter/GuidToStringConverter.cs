using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// Converter, which converts int from Db to date time.
    /// </summary>
    internal class GuidToStringConverter : IConverter
    {
        /// <summary>
        /// Converts the specified guid value from Db to Clr string value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public object Convert(object value)
        {
            return ((Guid)value).ToString("B");
        }

        /// <summary>
        /// Converts the string value from Clr to Db Guid value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// Converted value for Db.
        /// </returns>
        public object ConvertBack(object value)
        {
            return Guid.Parse((string)value);
        }
    }
}
