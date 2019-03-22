using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kros.KORM.Converter
{
    /// <summary>
    /// Interface, which describe converter, which know convert data from db to object and reverse.
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// Converts the specified value from Db to Clr.
        /// </summary>
        /// <param name="value">The value.</param>
        object Convert(object value);

        /// <summary>
        /// Converts the value from Clr to Db.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Converted value for Db.</returns>
        object ConvertBack(object value);
    }
}
