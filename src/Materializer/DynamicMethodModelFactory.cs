using Kros.Caching;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// Modelfactory, which materialize model by dynamic method delegates.
    /// </summary>
    /// <seealso cref="IModelFactory" />
    public class DynamicMethodModelFactory : IModelFactory
    {
        #region Private fields

        private readonly IDatabaseMapper _databaseMapper;
        private readonly ICache<int, Delegate> _factoriesCache = new Cache<int, Delegate>();
        private readonly ReaderKeyGenerator _keyGenerator = new ReaderKeyGenerator();
        private readonly ILogger<DynamicMethodModelFactory> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMethodModelFactory" /> class.
        /// </summary>
        /// <param name="databaseMapper">The database mapper.</param>
        /// <exception cref="System.ArgumentNullException">databaseMapper;Argument 'databaseMapper' is required.</exception>
        public DynamicMethodModelFactory(IDatabaseMapper databaseMapper)
        {
            _databaseMapper = Check.NotNull(databaseMapper, nameof(databaseMapper));
            _logger = KormLogging.CreateLogger<DynamicMethodModelFactory>();
        }

        #endregion

        /// <summary>
        /// Gets the factory for creating and filling model.
        /// </summary>
        /// <typeparam name="T">Type of model class.</typeparam>
        /// <param name="reader">Reader from fill model.</param>
        /// <returns>
        /// Factory for creating and filling model.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">reader;Argument 'reader' is required.</exception>
        public Func<IDataReader, T> GetFactory<T>(IDataReader reader)
        {
            Check.NotNull(reader, nameof(reader));

            int key = _keyGenerator.GenerateKey<T>(reader);
            _logger.LogDebug("Get factory for type '{type}' with key '{key}'.", typeof(T).FullName, key);

            return _factoriesCache.Get(key, () => CreateFactory<T>(reader)) as Func<IDataReader, T>;
        }

        private Func<IDataReader, T> CreateFactory<T>(IDataReader reader)
        {
            Type type = typeof(T);
            _logger.LogDebug("Create factory for type '{type}'.", typeof(T).FullName);
            if (type.IsValueType)
            {
                return new Func<IDataReader, T>(FactoryForValueType<T>);
            }
            else
            {
                TableInfo tableInfo = _databaseMapper.GetTableInfo<T>();
                IInjector injector = _databaseMapper.GetInjector<T>();
                (ConstructorInfo ctor, bool isDefault) = GetConstructor(type);

                if (isDefault)
                {
                    _logger.LogDebug("Found default constructor, generating factory with property setters.");
                    return CreateFactoryForPropertySetters<T>(reader, tableInfo, injector, ctor);
                }
                else
                {
                    _logger.LogDebug("Found non default constructor, generating factory for record type.");
                    RecordModelFactory recordModelFactory = new();
                    return recordModelFactory.CreateFactoryForRecords<T>(reader, tableInfo, injector, ctor);
                }
            }
        }

        private string GetFactoryName() => $"korm_factory_{_factoriesCache.Count}";

        private Func<IDataReader, T> CreateFactoryForPropertySetters<T>(
            IDataReader reader,
            TableInfo tableInfo,
            IInjector injector,
            ConstructorInfo ctor)
        {
            Type type = typeof(T);
            string factoryName = GetFactoryName();
            _logger.LogDebug("Start creating dynamic factory method '{factoryName}'.", factoryName);
            DynamicMethod dynamicMethod = new(factoryName, type, new Type[] { typeof(IDataReader) }, true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            LocalBuilder localResult = ilGenerator.DeclareLocal(typeof(T));
            ilGenerator.LogAndEmit(OpCodes.Newobj, ctor, _logger);
            ilGenerator.LogAndEmit(OpCodes.Stloc_S, localResult.LocalIndex, _logger);
            EmitReaderFields(reader, tableInfo, ilGenerator, injector);
            ilGenerator.CallOnAfterMaterialize(tableInfo, _logger);
            ilGenerator.LogAndEmit(OpCodes.Ldloc, localResult.LocalIndex, _logger);
            ilGenerator.LogAndEmit(OpCodes.Ret, _logger);
            _logger.LogDebug("End creating dynamic factory method '{factoryName}'.", factoryName);

            return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
        }

        // ToDo: Zrefaktorovať aby sa používal DynamicMethods.
        private static T FactoryForValueType<T>(IDataReader reader)
        {
            if (reader.IsDBNull(0))
            {
                return default;
            }

            Type destType = typeof(T);
            Type srcType = reader.GetFieldType(0);

            MethodInfo valueGetter = srcType.GetReaderValueGetter();

            object value = valueGetter.Invoke(reader, new object[] { 0 });
            if (destType.Name == srcType.Name)
            {
                return (T)value;
            }
            else
            {
                return (T)Convert.ChangeType(value, destType);
            }
        }

        private void EmitReaderFields(IDataReader reader,
            TableInfo tableInfo,
            ILGenerator ilGenerator,
            IInjector injector)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                EmitField(reader, tableInfo, ilGenerator, i);
            }
            EmitPropertyForInjecting(tableInfo, ilGenerator, injector);
        }

        private void EmitPropertyForInjecting(TableInfo tableInfo,
            ILGenerator ilGenerator,
            IInjector injector)
        {
            _logger.LogDebug("Emitting injectable properties.");
            foreach (PropertyInfo property in tableInfo
                .AllModelProperties
                .Where(p => injector.IsInjectable(p.Name)))
            {
                _logger.LogDebug("  {propertyName}", property.Name);
                ilGenerator.LogAndEmit(OpCodes.Ldloc_0, _logger);
                ilGenerator.CallGetInjectedValue(injector, property.Name, property.PropertyType, _logger);
                ilGenerator.LogAndEmit(OpCodes.Callvirt, property.GetSetMethod(true), _logger);
            }
        }

        private void EmitField(
            IDataReader reader,
            TableInfo tableInfo,
            ILGenerator ilGenerator,
            int columnIndex)
        {
            string fieldName = reader.GetName(columnIndex);
            ColumnInfo columnInfo = tableInfo.GetColumnInfo(fieldName);
            _logger.LogDebug("Emitting field {fieldIndex} from data reader: {fieldName}.", columnIndex, fieldName);
            if (columnInfo != null)
            {
                ilGenerator.LogAndEmit(OpCodes.Ldloc_0, _logger);
                Type srcType = reader.GetFieldType(columnIndex);
                _logger.LogDebug("  Field type is {type}.", srcType.FullName);
                IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);
                if (converter is null)
                {
                    _logger.LogDebug("  Field does not have a converter.");
                    ilGenerator.EmitFieldWithoutConverter(srcType, columnInfo.PropertyInfo.PropertyType, columnIndex, _logger);
                }
                else
                {
                    _logger.LogDebug("  Field has a converter.");
                    ilGenerator.EmitFieldWithConverter(converter, columnInfo.PropertyInfo.PropertyType, columnIndex, _logger);
                }
                ilGenerator.LogAndEmit(OpCodes.Callvirt, columnInfo.PropertyInfo.GetSetMethod(true), _logger);
            }
            else
            {
                _logger.LogDebug("Field was not emitted, because column '{fieldName}' was not found in table '{tableName}'.",
                    fieldName, tableInfo.Name);
            }
        }

        private static (ConstructorInfo ctor, bool isDefault) GetConstructor(Type type)
        {
            (ConstructorInfo ctor, bool isDefault) info = type.GetConstructor();

            if (info.ctor is null)
            {
                throw new InvalidOperationException(string.Format(Resources.Error_TooManyConstructors, type.FullName));
            }

            return info;
        }
    }
}
