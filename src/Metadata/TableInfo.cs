using Kros.KORM.Materializer;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Class, which represent information about table from database
    /// </summary>
    public class TableInfo
    {
        #region Private fields

        private Dictionary<string, ColumnInfo> _columns;
        private Lazy<Dictionary<string, ColumnInfo>> _properties;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TableInfo class.
        /// </summary>
        /// <param name="columns">The columns.</param>
        /// <param name="allModelProperties">All model properties</param>
        /// <param name="onAfterMaterialize">Method info accessor for calling OnAfterMaterialize over <seealso cref="IMaterialize"/>IMaterialize
        /// If Model doesn't implement <seealso cref="IMaterialize"/> then null.</param>
        /// <exception cref="ArgumentNullException">When columns is null.</exception>
        public TableInfo(IEnumerable<ColumnInfo> columns,
            IEnumerable<PropertyInfo> allModelProperties,
            MethodInfo onAfterMaterialize)
        {
            Check.NotNull(columns, nameof(columns));
            Check.NotNull(allModelProperties, nameof(allModelProperties));

            _columns = columns.ToDictionary(columnInfo => columnInfo.Name,
                                            columnInfo => columnInfo,
                                            StringComparer.CurrentCultureIgnoreCase);
            this.OnAfterMaterialize = onAfterMaterialize;
            this.AllModelProperties = allModelProperties;

            _properties = new Lazy<Dictionary<string, ColumnInfo>>(() =>
                   _columns.ToDictionary(columnInfo => columnInfo.Value.PropertyInfo.Name,
                           columnInfo => columnInfo.Value,
                           StringComparer.CurrentCultureIgnoreCase));
        }

        #endregion

        #region Public Property

        /// <summary>
        /// All model properties.
        /// </summary>
        public IEnumerable<PropertyInfo> AllModelProperties { get; private set; }

        /// <summary>
        /// Method info accessor for calling OnAfterMaterialize over <seealso cref="IMaterialize"/>IMaterialize
        /// If Model doesn't implement <seealso cref="IMaterialize"/> then null.
        /// </summary>
        public MethodInfo OnAfterMaterialize { get; private set; }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the columns, which are part of primary key.
        /// </summary>
        public IEnumerable<ColumnInfo> PrimaryKey
            => _columns.Values.Where(column => column.IsPrimaryKey).OrderBy(column => column.PrimaryKeyOrder);

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public IEnumerable<ColumnInfo> Columns => _columns.Values;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the column information.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns>
        /// Column information.
        /// </returns>
        /// <exception cref="ArgumentNullException">When columnName is null.</exception>
        public ColumnInfo GetColumnInfo(string columnName)
        {
            Check.NotNull(columnName, nameof(columnName));
            return _columns.TryGetValue(columnName, out ColumnInfo column) ? column : null;
        }

        /// <summary>
        /// Gets the column information.
        /// </summary>
        /// <param name="property">Property Info.</param>
        /// <returns>
        /// Column information.
        /// </returns>
        /// <exception cref="ArgumentNullException">When property is null.</exception>
        public ColumnInfo GetColumnInfo(PropertyInfo property) => GetColumnInfoByPropertyName(property.Name);

        /// <summary>
        /// Gets the name of the column information by property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public ColumnInfo GetColumnInfoByPropertyName(string propertyName)
            => _properties.Value.TryGetValue(propertyName, out ColumnInfo column) ? column : null;

        #endregion
    }
}
