using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Kros.KORM.Injection;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Interface, which describe class for mapping database to model.
    /// </summary>
    public interface IModelMapper
    {
        /// <summary>
        /// Gets or sets the column name mapping logic.
        /// </summary>
        /// <remarks>
        /// Params:
        ///     ColumnInfo - info about column.
        ///     Type - Type of model.
        ///     string - return column name.
        /// </remarks>
        Func<ColumnInfo, Type, string> MapColumnName { get; set; }

        /// <summary>
        /// Set column name for specific property.
        /// </summary>
        /// <param name="modelProperty">Expression for defined property to.</param>
        /// <param name="columnName">Database column name.</param>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\ModelMapperExample.cs" title="SetColumnName" region="SetColumnName" language="cs" />
        /// </example>
        void SetColumnName<TModel, TValue>(Expression<Func<TModel, TValue>> modelProperty, string columnName) where TModel : class;

        /// <summary>
        /// Gets or sets the table name mapping logic.
        /// </summary>
        /// <remarks>
        /// Params:
        ///     TableInfo - info about table.
        ///     Type - Type of model.
        ///     string - return table name.
        /// </remarks>
        Func<TableInfo, Type, string> MapTableName { get; set; }

        /// <summary>
        /// Gets or sets the primary key mapping logic.
        /// </summary>
        Func<TableInfo, IEnumerable<ColumnInfo>> MapPrimaryKey { get; set; }

        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <returns>Table info.</returns>
        TableInfo GetTableInfo<T>();

        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>
        /// Table info.
        /// </returns>
        TableInfo GetTableInfo(Type modelType);

        /// <summary>
        /// Get property injection configuration for model T.
        /// </summary>
        /// <example>
        /// <code source="..\..\..\..\Documentation\Examples\Kros.KORM.Examples\WelcomeExample.cs" title="Injection" region="InectionConfiguration" language="cs" />
        /// </example>
        IInjectionConfigurator<T> InjectionConfigurator<T>();

        /// <summary>
        /// Get property service injector.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>Service property injector.</returns>
        IInjector GetInjector<T>();
    }
}