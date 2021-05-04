using Kros.KORM.Converter;
using System;
using System.Reflection;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Class, which represent information about column from database.
    /// </summary>
    public class ColumnInfo
    {
        private PropertyInfo _propertyInfo;

        /// <summary>
        /// Column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the property information.
        /// </summary>
        public PropertyInfo PropertyInfo
        {
            get => _propertyInfo;
            set
            {
                _propertyInfo = value;
                DefaultValue = null;
                if (_propertyInfo is not null)
                {
                    IsNullable = Nullable.GetUnderlyingType(PropertyInfo.PropertyType) != null;
                    if (_propertyInfo.PropertyType.IsValueType)
                    {
                        DefaultValue = Activator.CreateInstance(PropertyInfo.PropertyType);
                    }
                }
            }
        }

        /// <summary>
        /// Default value for the data type of the column.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Checks if <paramref name="value"/> is default value of the column.
        /// </summary>
        /// <param name="value">Checked value.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is default value for the column,
        /// otherwise <see langword="false"/>.</returns>
        public bool IsDefaultValue(object value) => value is null || value.Equals(DefaultValue);

        /// <summary>
        /// Gets or sets the data converter.
        /// </summary>
        public IConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets when value is generated for this column.
        /// </summary>
        public ValueGenerated ValueGenerated { get; set; }

        /// <summary>
        /// Gets or sets value generator for this column.
        /// </summary>
        public IValueGenerator ValueGenerator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this column is primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets the order of the column, if it is in composite primary key.
        /// </summary>
        public int PrimaryKeyOrder { get; set; }

        /// <summary>
        /// Name of the generator. If not set, table name will be used.
        /// </summary>
        public string AutoIncrementGeneratorName { get; set; }

        /// <summary>
        /// Type of primary key auto increment method.
        /// </summary>
        public AutoIncrementMethodType AutoIncrementMethodType { get; set; } = AutoIncrementMethodType.None;

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="value">The value.</param>
        public void SetValue(object targetObject, object value) => PropertyInfo.SetValue(targetObject, value, null);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <returns>
        /// Return value from targetObject.
        /// </returns>
        public object GetValue(object targetObject) => PropertyInfo.GetValue(targetObject, null);

        /// <summary>
        /// Gets a value indicating whether property has nullable type.
        /// </summary>
        public bool IsNullable { get; private set; }
    }
}
