using Kros.Caching;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using Kros.KORM.Properties;
using Kros.Utils;
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

            return _factoriesCache.Get(key, () => CreateFactory<T>(reader)) as Func<IDataReader, T>;
        }

        private Func<IDataReader, T> CreateFactory<T>(IDataReader reader)
        {
            Type type = typeof(T);
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
                    return CreateFactoryForPropertySetters<T>(reader, tableInfo, injector, ctor);
                }
                else
                {
                    return RecordModelFactory.CreateFactoryForRecords<T>(reader, tableInfo, injector, ctor);
                }
            }
        }

        private string GetFactoryName => $"korm_factory_{_factoriesCache.Count}";

        private Func<IDataReader, T> CreateFactoryForPropertySetters<T>(
            IDataReader reader,
            TableInfo tableInfo,
            IInjector injector,
            ConstructorInfo ctor)
        {
            Type type = typeof(T);
            var dynamicMethod = new DynamicMethod(GetFactoryName, type, new Type[] { typeof(IDataReader) }, true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Newobj, ctor);

            EmitReaderFields(reader, tableInfo, ilGenerator, injector);

            ilGenerator.CallOnAfterMaterialize(tableInfo);

            ilGenerator.Emit(OpCodes.Ret);

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

        private static void EmitReaderFields(IDataReader reader,
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

        private static void EmitPropertyForInjecting(TableInfo tableInfo,
            ILGenerator ilGenerator,
            IInjector injector)
        {
            foreach (PropertyInfo property in tableInfo
                .AllModelProperties
                .Where(p => injector.IsInjectable(p.Name)))
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.CallGetInjectedValue(injector, property.Name, property.PropertyType);
                ilGenerator.Emit(OpCodes.Callvirt, property.GetSetMethod(true));
            }
        }

        private static void EmitField(
            IDataReader reader,
            TableInfo tableInfo,
            ILGenerator ilGenerator,
            int columnIndex)
        {
            ColumnInfo columnInfo = tableInfo.GetColumnInfo(reader.GetName(columnIndex));
            if (columnInfo != null)
            {
                Type srcType = reader.GetFieldType(columnIndex);
                IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);
                if (converter is null)
                {
                    EmitFieldWithoutConverter(ilGenerator, srcType, columnInfo, columnIndex);
                }
                else
                {
                    EmitFieldWithConverter(ilGenerator, converter, columnInfo, columnIndex);
                }
            }
        }

        private static void EmitFieldWithoutConverter(
            ILGenerator ilGenerator,
            Type srcType,
            ColumnInfo columnInfo,
            int columnIndex)
        {
            Label truePart = ilGenerator.CallReaderIsDbNull(columnIndex);
            ilGenerator.CallReaderGetValueWithoutConverter(columnIndex, columnInfo, srcType);
            ilGenerator.Emit(OpCodes.Callvirt, columnInfo.PropertyInfo.GetSetMethod(true));
            ilGenerator.MarkLabel(truePart);
        }

        private static void EmitFieldWithConverter(
            ILGenerator ilGenerator,
            IConverter converter,
            ColumnInfo columnInfo,
            int columnIndex)
        {
            Label truePart = ilGenerator.CallReaderIsDbNull(columnIndex);
            ilGenerator.CallReaderGetValueWithConverter(columnIndex, converter, columnInfo);
            ilGenerator.Emit(OpCodes.Callvirt, columnInfo.PropertyInfo.GetSetMethod(true));
            ilGenerator.MarkLabel(truePart);
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
