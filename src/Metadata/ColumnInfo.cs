using Kros.KORM.Converter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Class, which represent information about column from database.
    /// </summary>
    public class ColumnInfo
    {
        #region Public Property

        /// <summary>
        /// Column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the property information.
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// Gets or sets the data converter.
        /// </summary>
        public IConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this column is primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets the order of the column, if it is in composite primary key.
        /// </summary>
        public int PrimaryKeyOrder { get; set; }

        /// <summary>
        /// Type of primary key auto increment method.
        /// </summary>
        public AutoIncrementMethodType AutoIncrementMethodType { get; set; } = AutoIncrementMethodType.None;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="value">The value.</param>
        public void SetValue(object targetObject, object value)
        {
            this.PropertyInfo.SetValue(targetObject, value, null);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <returns>
        /// Return value from targetObject.
        /// </returns>
        public object GetValue(object targetObject)
        {
            return this.PropertyInfo.GetValue(targetObject, null);
        }

        #endregion
    }
}
