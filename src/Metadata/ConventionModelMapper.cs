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
    public partial class ConventionModelMapper : IModelMapper, IModelMapperInternal
    {
        private const string ConventionalPrimaryKeyName = "ID";

        private readonly Dictionary<Type, EntityMapper> _entities = new Dictionary<Type, EntityMapper>();

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

        /// <summary>
        /// Get property injection configuration for model T.
        /// </summary>
        /// <example>
        /// <code source="..\..\Documentation\Examples\Kros.KORM.Examples\WelcomeExample.cs" title="Injection" region="InectionConfiguration" language="cs" />
        /// </example>
        public IInjectionConfigurator<T> InjectionConfigurator<T>()
        {
            EntityMapper entity = GetEntity<T>();
            if (entity.Injector is null)
            {
                entity.Injector = new InjectionConfiguration<T>();
            }
            return (IInjectionConfigurator<T>)entity.Injector;
        }

        /// <summary>
        /// Get property service injector.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <returns>Service property injector.</returns>
        public IInjector GetInjector<T>() => GetInjector(typeof(T));

        private IInjector GetInjector(Type modelType)
        {
            if (_entities.TryGetValue(modelType, out EntityMapper entity))
            {
                return entity.Injector ?? DummyInjector.Default;
            }
            else
            {
                return DummyInjector.Default;
            }
        }

        #endregion

        #region Private Helpers

        private TableInfo CreateTableInfo(Type modelType)
        {
            _entities.TryGetValue(modelType, out EntityMapper entity);
            IInjector injector = GetInjector(modelType);

            PropertyInfo[] allModelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            IEnumerable<PropertyInfo> columnProperties = allModelProperties
                .Where(p =>
                {
                    return p.CanWrite
                        && ((entity is null) || !entity.NoMap.Contains(p.Name))
                        && (p.GetCustomAttributes(typeof(NoMapAttribute), true).Length == 0)
                        && !injector.IsInjectable(p.Name);
                });

            IEnumerable<ColumnInfo> columns = columnProperties.Select(p => CreateColumnInfo(p, modelType));
            MethodInfo onAfterMaterialize = GetOnAfterMaterializeInfo(modelType);
            var tableInfo = new TableInfo(columns, allModelProperties, onAfterMaterialize);

            tableInfo.Name = GetTableName(tableInfo, modelType);

            SetPrimaryKey(tableInfo, modelType);

            return tableInfo;
        }

        private void SetPrimaryKey(TableInfo tableInfo, Type modelType)
        {
            if (_entities.TryGetValue(modelType, out EntityMapper entity) && (entity.PrimaryKeyPropertyName != null))
            {
                ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(entity.PrimaryKeyPropertyName);
                columnInfo.IsPrimaryKey = true;
                columnInfo.AutoIncrementMethodType = entity.PrimaryKeyAutoIncrementType;
            }
            else
            {
                foreach (ColumnInfo key in MapPrimaryKey(tableInfo))
                {
                    key.IsPrimaryKey = true;
                }
            }
        }

        private MethodInfo GetOnAfterMaterializeInfo(Type modelType)
        {
            MethodInfo onAfterMaterialize = null;

            if (typeof(IMaterialize).IsAssignableFrom(modelType))
            {
                onAfterMaterialize = typeof(IMaterialize).GetMethod(nameof(IMaterialize.OnAfterMaterialize));
            }

            return onAfterMaterialize;
        }

        private string GetTableName(TableInfo tableInfo, Type modelType)
        {
            string name;

            if (_entities.TryGetValue(modelType, out EntityMapper entity) && (entity.TableName != null))
            {
                name = entity.TableName;
            }
            else
            {
                name = GetName(modelType);
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
            if (_entities.TryGetValue(modelType, out EntityMapper entity))
            {
                if (entity.Converters.TryGetValue(columnInfo.PropertyInfo.Name, out IConverter converter)
                    || entity.PropertyConverters.TryGetValue(propertyInfo.PropertyType, out converter))
                {
                    columnInfo.Converter = converter == NoConverter.Instance ? null : converter;
                }
            }
            if (columnInfo.Converter is null)
            {
                columnInfo.Converter = GetConverterFromAttribute(propertyInfo);
            }
        }

        private string GetColumnName(ColumnInfo columnInfo, Type modelType)
        {
            if (_entities.TryGetValue(modelType, out EntityMapper entity)
                && entity.ColumnMap.TryGetValue(columnInfo.PropertyInfo.Name, out string columnName))
            {
                return columnName;
            }

            var name = GetName(columnInfo.PropertyInfo);
            return string.IsNullOrWhiteSpace(name) ? MapColumnName(columnInfo, modelType) : name;
        }

        private IConverter GetConverterFromAttribute(PropertyInfo propertyInfo)
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

        private EntityMapper GetEntity<TEntity>()
        {
            Type entityType = typeof(TEntity);
            if (!_entities.TryGetValue(entityType, out EntityMapper entity))
            {
                entity = new EntityMapper(entityType);
                _entities.Add(entityType, entity);
            }
            return entity;
        }

        void IModelMapperInternal.SetTableName<TEntity>(string tableName) => GetEntity<TEntity>().TableName = tableName;

        void IModelMapperInternal.SetColumnName<TEntity>(string propertyName, string columnName)
        {
            Check.NotNullOrWhiteSpace(propertyName, nameof(propertyName));
            Check.NotNullOrWhiteSpace(columnName, nameof(columnName));
            EntityMapper entity = GetEntity<TEntity>();
            if (entity.ColumnMap.TryGetValue(propertyName, out string currentMapping))
            {
                ThrowHelper.ColumnMappingAlreadyConfigured<TEntity>(propertyName, columnName, currentMapping);
            }
            entity.ColumnMap.Add(propertyName, columnName);
        }

        void IModelMapperInternal.SetNoMap<TEntity>(string propertyName) => GetEntity<TEntity>().NoMap.Add(propertyName);

        void IModelMapperInternal.SetConverter<TEntity>(string propertyName, IConverter converter)
        {
            Check.NotNullOrWhiteSpace(propertyName, nameof(propertyName));
            Check.NotNull(converter, nameof(converter));
            EntityMapper entity = GetEntity<TEntity>();
            if (entity.Converters.TryGetValue(propertyName, out IConverter currentConverter))
            {
                ThrowHelper.ConverterAlreadyConfigured<TEntity>(propertyName, converter, currentConverter);
            }
            entity.Converters.Add(propertyName, converter);
        }

        void IModelMapperInternal.SetConverterForProperties<TEntity>(Type propertyType, IConverter converter)
        {
            Check.NotNull(converter, nameof(converter));
            EntityMapper entity = GetEntity<TEntity>();
            if (entity.PropertyConverters.TryGetValue(propertyType, out IConverter currentConverter))
            {
                ThrowHelper.ConverterForTypeAlreadyConfigured<TEntity>(propertyType, converter, currentConverter);
            }
            entity.PropertyConverters.Add(propertyType, converter);
        }

        void IModelMapperInternal.SetInjector<TEntity>(IInjector injector) => GetEntity<TEntity>().Injector = injector;

        void IModelMapperInternal.SetPrimaryKey<TEntity>(string propertyName, AutoIncrementMethodType autoIncrementType)
        {
            EntityMapper entity = GetEntity<TEntity>();
            entity.PrimaryKeyPropertyName = propertyName;
            entity.PrimaryKeyAutoIncrementType = autoIncrementType;
        }

        #endregion
    }
}
