using Kros.KORM.Converter;
using Kros.KORM.Exceptions;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Properties;
using Kros.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kros.KORM.Metadata
{
    /// <summary>
    /// Model mapper, which know define convention for name mapping.
    /// </summary>
    /// <seealso cref="IModelMapper" />
    public class ConventionModelMapper : IModelMapper, IModelMapperInternal
    {
        private const string ConventionalPrimaryKeyName = "ID";
        private static readonly string _onAfterMaterializeName = nameof(IMaterialize.OnAfterMaterialize);
        private readonly Dictionary<Type, Dictionary<string, string>> _columnMap =
            new Dictionary<Type, Dictionary<string, string>>();
        private readonly Dictionary<Type, Dictionary<string, IConverter>> _converters =
            new Dictionary<Type, Dictionary<string, IConverter>>();
        private readonly Dictionary<Type, string> _tableMap = new Dictionary<Type, string>();
        private readonly Dictionary<Type, (string propertyName, AutoIncrementMethodType methodType)> _keys
            = new Dictionary<Type, (string, AutoIncrementMethodType)>();
        private readonly HashSet<string> _noMap = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionModelMapper"/> class.
        /// </summary>
        public ConventionModelMapper()
        {
            MapColumnName = (columnInfo, type) =>
            {
                return columnInfo.PropertyInfo.Name;
            };

            MapTableName = (tableInfo, tableType) =>
            {
                return tableType.Name;
            };

            MapPrimaryKey = (tableInfo) =>
            {
                return OnMapPrimaryKey(tableInfo);
            };
        }

        /// <summary>
        /// Gets or sets the column name mapping logic.
        /// </summary>
        /// <remarks>
        /// Params:
        ///     ColumnInfo - info about column.
        ///     Type - Type of model.
        ///     string - return column name.
        /// </remarks>
        public Func<ColumnInfo, Type, string> MapColumnName { get; set; }

        /// <summary>
        /// Set column name for specific property.
        /// </summary>
        /// <param name="modelProperty">Expression for defined property to.</param>
        /// <param name="columnName">Database column name.</param>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\ModelMapperExample.cs" title="SetColumnName" region="SetColumnName" language="cs" />
        /// </example>
        public void SetColumnName<TModel, TValue>(Expression<Func<TModel, TValue>> modelProperty, string columnName)
            where TModel : class
        {
            Check.NotNull(modelProperty, nameof(modelProperty));
            Check.NotNullOrEmpty(columnName, nameof(columnName));

            ((IModelMapperInternal)this).SetColumnName<TModel>(PropertyName<TModel>.GetPropertyName(modelProperty), columnName);
        }

        /// <summary>
        /// Gets or sets the table name mapping logic.
        /// </summary>
        public Func<TableInfo, Type, string> MapTableName { get; set; }

        /// <summary>
        /// Gets or sets the primary key mapping logic.
        /// </summary>
        public Func<TableInfo, IEnumerable<ColumnInfo>> MapPrimaryKey { get; set; }

        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <typeparam name="T">Type of model.</typeparam>
        /// <returns>
        /// Table info.
        /// </returns>
        public TableInfo GetTableInfo<T>() => CreateTableInfo(typeof(T));

        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <param name="modelType">Type of the model.</param>
        /// <returns>
        /// Table info.
        /// </returns>
        public TableInfo GetTableInfo(Type modelType) => CreateTableInfo(modelType);

        #region Injection

        private readonly Dictionary<Type, IInjector> _injectors = new Dictionary<Type, IInjector>();

        /// <summary>
        /// Get property injection configuration for model T.
        /// </summary>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\WelcomeExample.cs" title="Injection" region="InectionConfiguration" language="cs" />
        /// </example>
        public IInjectionConfigurator<T> InjectionConfigurator<T>()
        {
            if (!_injectors.TryGetValue(typeof(T), out IInjector injector))
            {
                injector = new InjectionConfiguration<T>();
                _injectors.Add(typeof(T), injector);
            }
            return (IInjectionConfigurator<T>)injector;
        }

        /// <summary>
        /// Get property service injector.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>Service property injector.</returns>
        public IInjector GetInjector<T>() => GetInjector(typeof(T));

        private IInjector GetInjector(Type modelType)
        {
            if (_injectors.ContainsKey(modelType))
            {
                return _injectors[modelType];
            }
            else
            {
                return DummyInjector.Default;
            }
        }

        private class DummyInjector : IInjector
        {
            public static IInjector Default { get; } = new DummyInjector();

            public object GetValue(string propertyName) =>
                throw new NotImplementedException();

            public bool IsInjectable(string propertyName) => false;
        }

        #endregion

        #region Private Helpers

        private TableInfo CreateTableInfo(Type modelType)
        {
            IInjector injector = GetInjector(modelType);
            IEnumerable<PropertyInfo> properties = GetModelProperties(modelType)
                .Where(p =>
                {
                    return p.CanWrite
                        && !_noMap.Contains(GetPropertyKey(modelType, p.Name))
                        && (p.GetCustomAttributes(typeof(NoMapAttribute), true).Length == 0)
                        && !injector.IsInjectable(p.Name);
                });

            IEnumerable<ColumnInfo> columns = properties.Select(p => CreateColumnInfo(p, modelType));
            MethodInfo onAfterMaterialize = GetOnAfterMaterializeInfo(modelType);
            var tableInfo = new TableInfo(columns, GetModelProperties(modelType), onAfterMaterialize);

            tableInfo.Name = GetTableName(tableInfo, modelType);

            SetPrimaryKey(tableInfo, modelType);

            return tableInfo;
        }

        private void SetPrimaryKey(TableInfo tableInfo, Type modelType)
        {
            if (_keys.ContainsKey(modelType))
            {
                (string propertyName, AutoIncrementMethodType methodType) = _keys[modelType];
                ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(propertyName);
                columnInfo.IsPrimaryKey = true;
                columnInfo.AutoIncrementMethodType = methodType;
            }
            else
            {
                foreach (ColumnInfo key in MapPrimaryKey(tableInfo))
                {
                    key.IsPrimaryKey = true;
                }
            }
        }

        private static string GetPropertyKey(Type modelType, string propertyName)
            => $"{modelType.FullName}-{propertyName}";

        private static PropertyInfo[] GetModelProperties(Type modelType) =>
            modelType.GetProperties(BindingFlags.Public |
                BindingFlags.GetProperty |
                BindingFlags.SetProperty |
                BindingFlags.Instance);

        private MethodInfo GetOnAfterMaterializeInfo(Type modelType)
        {
            MethodInfo onAfterMaterialize = null;

            if (typeof(IMaterialize).IsAssignableFrom(modelType))
            {
                onAfterMaterialize = typeof(IMaterialize).GetMethod(_onAfterMaterializeName);
            }

            return onAfterMaterialize;
        }

        private string GetTableName(TableInfo tableInfo, Type modelType)
        {
            var name = GetName(modelType);

            if (_tableMap.ContainsKey(modelType))
            {
                name = _tableMap[modelType];
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = MapTableName(tableInfo, modelType);
            }

            return name;
        }

        private ColumnInfo CreateColumnInfo(PropertyInfo propertyInfo, Type modelType)
        {
            var columnInfo = new ColumnInfo { PropertyInfo = propertyInfo };
            columnInfo.Name = GetColumnName(columnInfo, modelType);

            SetConverter(propertyInfo, columnInfo, modelType);

            return columnInfo;
        }

        private void SetConverter(PropertyInfo propertyInfo, ColumnInfo columnInfo, Type modelType)
        {
            IConverter converter = GetConverter(propertyInfo);

            if (_converters.ContainsKey(modelType) && _converters[modelType].ContainsKey(columnInfo.PropertyInfo.Name))
            {
                converter = _converters[modelType][columnInfo.PropertyInfo.Name];
            }

            columnInfo.Converter = converter;
        }

        private string GetColumnName(ColumnInfo columnInfo, Type modelType)
        {
            var name = GetName(columnInfo.PropertyInfo);

            if (_columnMap.ContainsKey(modelType) && _columnMap[modelType].ContainsKey(columnInfo.PropertyInfo.Name))
            {
                name = _columnMap[modelType][columnInfo.PropertyInfo.Name];
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = MapColumnName(columnInfo, modelType);
            }

            return name;
        }

        private IConverter GetConverter(PropertyInfo propertyInfo)
        {
            var attributes = propertyInfo.GetCustomAttributes(typeof(ConverterAttribute), true);
            if (attributes.Length == 1)
            {
                return (attributes[0] as ConverterAttribute).Converter;
            }
            else
            {
                return null;
            }
        }

        private string GetName(ICustomAttributeProvider attributeProvider)
        {
            var aliasAttr = attributeProvider.GetCustomAttributes(typeof(AliasAttribute), true)
                .FirstOrDefault() as AliasAttribute;

            return aliasAttr?.Alias;
        }

        private static IEnumerable<ColumnInfo> OnMapPrimaryKey(TableInfo tableInfo)
        {
            ColumnInfo pkByConvention = null;
            var pkByAttributes = new List<(ColumnInfo Column, KeyAttribute Attribute)>();

            foreach (ColumnInfo column in tableInfo.Columns)
            {
                var attributes = column.PropertyInfo.GetCustomAttributes(typeof(KeyAttribute), true);
                if (attributes.Length == 1)
                {
                    column.IsPrimaryKey = true;
                    pkByAttributes.Add((column, attributes[0] as KeyAttribute));
                }
                else if (column.Name.Equals(ConventionalPrimaryKeyName, StringComparison.OrdinalIgnoreCase))
                {
                    pkByConvention = column;
                }
            }

            var ret = new List<ColumnInfo>();
            if (pkByAttributes.Count == 1)
            {
                ret.Add(pkByAttributes[0].Column);
                ret[0].AutoIncrementMethodType = pkByAttributes[0].Attribute.AutoIncrementMethodType;
            }
            else if (pkByAttributes.Count > 1)
            {
                CheckPrimaryKeyColumns(pkByAttributes, tableInfo.Name);
                ret.AddRange(pkByAttributes.OrderBy(item => item.Attribute.Order).Select(item => item.Column));
                for (int i = 0; i < ret.Count; i++)
                {
                    ret[i].PrimaryKeyOrder = i;
                }
            }
            else if (pkByConvention != null)
            {
                pkByConvention.IsPrimaryKey = true;
                ret.Add(pkByConvention);
            }

            return ret;
        }

        private static void CheckPrimaryKeyColumns(
            List<(ColumnInfo Column, KeyAttribute Attribute)> pkAttributes,
            string tableName)
        {
            if (pkAttributes.Select(item => item.Attribute.Order).Distinct().Count() != pkAttributes.Count)
            {
                throw new CompositePrimaryKeyException(Resources.CompositePrimaryKeyMustHaveOrderedColumns, tableName);
            }

            if (pkAttributes.Select(item => item.Attribute.Name).Distinct().Count() > 1)
            {
                throw new CompositePrimaryKeyException(Resources.CompositePrimaryKeyMustHaveSameNameInAllColumns, tableName);
            }

            if (pkAttributes.Any(item => item.Attribute.AutoIncrementMethodType != AutoIncrementMethodType.None))
            {
                throw new CompositePrimaryKeyException(
                    string.Format(
                        Resources.CompositePrimaryKeyCanNotHaveAutoIncrementColumn,
                        nameof(KeyAttribute.AutoIncrementMethodType),
                        nameof(AutoIncrementMethodType.None)),
                    tableName);
            }
        }

        #endregion

        #region IModelMapperInternal

        void IModelMapperInternal.SetTableName<TEntity>(string tableName) => _tableMap[typeof(TEntity)] = tableName;

        void IModelMapperInternal.SetColumnName<TEntity>(string propertyName, string columnName)
        {
            if (!_columnMap.ContainsKey(typeof(TEntity)))
            {
                _columnMap[typeof(TEntity)] = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            }

            _columnMap[typeof(TEntity)][propertyName] = columnName;
        }

        void IModelMapperInternal.SetNoMap<TEntity>(string propertyName)
            => _noMap.Add(GetPropertyKey(typeof(TEntity), propertyName));

        void IModelMapperInternal.SetConverter<TEntity>(string propertyName, IConverter converter)
        {
            if (!_converters.ContainsKey(typeof(TEntity)))
            {
                _converters[typeof(TEntity)] = new Dictionary<string, IConverter>(StringComparer.CurrentCultureIgnoreCase);
            }
            _converters[typeof(TEntity)][propertyName] = converter;
        }

        void IModelMapperInternal.SetInjector<TEntity>(IInjector injector) => _injectors[typeof(TEntity)] = injector;

        void IModelMapperInternal.SetPrimaryKey<TEntity>(string propertyName, AutoIncrementMethodType autoIncrementType)
            => _keys[typeof(TEntity)] = (propertyName, autoIncrementType);

        #endregion
    }
}
