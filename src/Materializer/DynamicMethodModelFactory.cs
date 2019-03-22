using Kros.Caching;
using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using Kros.Utils;
using System;
using System.Collections.Generic;
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
    /// <seealso cref="Kros.KORM.Materializer.IModelFactory" />
    public class DynamicMethodModelFactory : IModelFactory
    {
        #region Private fields

        private IDatabaseMapper _databaseMapper;
        private ICache<int, Delegate> _factoriesCache = new Cache<int, Delegate>();
        private ReaderKeyGenerator _keyGenerator = new ReaderKeyGenerator();
        private readonly static List<IConverter> _converters = new List<IConverter>();
        private readonly static List<IInjector> _injectors = new List<IInjector>();

        private readonly static MethodInfo _fnIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        private readonly static MethodInfo _fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private readonly static MethodInfo _fnConvert = typeof(IConverter).GetMethod("Convert");
        private readonly static FieldInfo _fldConverters = typeof(DynamicMethodModelFactory).GetField(nameof(_converters),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static FieldInfo _fldInjectors = typeof(DynamicMethodModelFactory).GetField(nameof(_injectors),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static MethodInfo _fnConvertersListGetItem = typeof(List<IConverter>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnInjectorsListGetItem = typeof(List<IInjector>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnInjectorMethodInfo =
            typeof(IInjector).GetMethod(nameof(IInjector.GetValue), new Type[] { typeof(string) });
        private readonly static Dictionary<string, MethodInfo> _readerValueGetters = InitReaderValueGetters();
        private readonly static MethodInfo _getValueMethodInfo =
            typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });

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

            var key = _keyGenerator.GenerateKey<T>(reader);

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
                var injector = _databaseMapper.GetInjector<T>();

                var dynamicMethod = new DynamicMethod(string.Format("korm_factory_{0}", _factoriesCache.Count),
                    type, new Type[] { typeof(IDataReader) }, true);
                var ilGenerator = dynamicMethod.GetILGenerator();

                ConstructorInfo ctor = GetConstructor(type);
                ilGenerator.Emit(OpCodes.Newobj, ctor);

                EmitReaderFields(reader, tableInfo, ilGenerator, injector);

                CallOnAfterMaterialize(tableInfo, ilGenerator);

                ilGenerator.Emit(OpCodes.Ret);

                return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
            }
        }

        // ToDo: Zrefaktorovať aby sa používal DynamicMethods.
        private static T FactoryForValueType<T>(IDataReader reader)
        {
            Type destType = typeof(T);
            Type srcType = reader.GetFieldType(0);

            var valueGetter = GetReaderValueGetter(srcType);

            var value = valueGetter.Invoke(reader, new object[] { 0 });
            if (destType.Name == srcType.Name)
            {
                return (T)value;
            }
            else
            {
                return (T)Convert.ChangeType(value, destType);
            }
        }

        private static void CallOnAfterMaterialize(TableInfo tableInfo, ILGenerator ilGenerator)
        {
            if (tableInfo.OnAfterMaterialize != null)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                if (tableInfo.OnAfterMaterialize.IsVirtual)
                {
                    ilGenerator.Emit(OpCodes.Callvirt, tableInfo.OnAfterMaterialize);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Call, tableInfo.OnAfterMaterialize);
                }
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
            foreach (var property in tableInfo
                .AllModelProperties
                .Where(p => injector.IsInjectable(p.Name)))
            {
                int injectorIndex = GetInjectorIndex(injector);

                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldsfld, _fldInjectors);
                ilGenerator.Emit(OpCodes.Ldc_I4, injectorIndex);
                ilGenerator.Emit(OpCodes.Callvirt, _fnInjectorsListGetItem);

                ilGenerator.Emit(OpCodes.Ldstr, property.Name);
                ilGenerator.Emit(OpCodes.Callvirt, _fnInjectorMethodInfo);

                ilGenerator.Emit(OpCodes.Unbox_Any, property.PropertyType);
                ilGenerator.Emit(OpCodes.Callvirt, property.GetSetMethod(true));
            }
        }

        private static int GetInjectorIndex(IInjector injector)
        {
            var injectorIndex = _injectors.IndexOf(injector);
            if (injectorIndex == -1)
            {
                _injectors.Add(injector);
                injectorIndex = _injectors.Count - 1;
            }

            return injectorIndex;
        }

        private void EmitField(IDataReader reader,
            TableInfo tableInfo,
            ILGenerator ilGenerator,
            int columnIndex)
        {
            var columnInfo = tableInfo.GetColumnInfo(reader.GetName(columnIndex));
            if (columnInfo != null)
            {
                var srcType = reader.GetFieldType(columnIndex);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4, columnIndex);
                ilGenerator.Emit(OpCodes.Callvirt, _fnIsDBNull);
                var lblNext = ilGenerator.DefineLabel();
                ilGenerator.Emit(OpCodes.Brtrue_S, lblNext);

                ilGenerator.Emit(OpCodes.Dup);
                var converter = ConverterHelper.GetConverter(columnInfo, srcType);

                if (converter == null)
                {
                    FillingValuesWithoutConverter(ilGenerator, columnIndex, columnInfo, srcType);
                }
                else
                {
                    FillingValuesWithConverter(ilGenerator, columnIndex, columnInfo, converter);
                }

                ilGenerator.MarkLabel(lblNext);
            }
        }

        private static ConstructorInfo GetConstructor(Type type)
        {
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null, new Type[0], null);
            if (ctor == null)
            {
                throw new InvalidOperationException($"Type '[{type.FullName}]' should have default public or non-public constructor");
            }

            return ctor;
        }

        private static void FillingValuesWithConverter(ILGenerator ilGenerator,
                                                               int fieldIndex,
                                                        ColumnInfo columnInfo,
                                                        IConverter converter)
        {
            int converterIndex = _converters.Count;
            _converters.Add(converter);

            ilGenerator.Emit(OpCodes.Ldsfld, _fldConverters);
            ilGenerator.Emit(OpCodes.Ldc_I4, converterIndex);
            ilGenerator.Emit(OpCodes.Callvirt, _fnConvertersListGetItem);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            ilGenerator.Emit(OpCodes.Callvirt, _fnGetValue);

            ilGenerator.Emit(OpCodes.Callvirt, _fnConvert);

            ilGenerator.Emit(OpCodes.Unbox_Any, columnInfo.PropertyInfo.PropertyType);
            ilGenerator.Emit(OpCodes.Callvirt, columnInfo.PropertyInfo.GetSetMethod(true));
        }

        private static void FillingValuesWithoutConverter(ILGenerator ilGenerator,
                                                                  int fieldIndex,
                                                           ColumnInfo columnInfo,
                                                                 Type srcType)
        {
            bool castNeeded = false;

            MethodInfo valuegetter = GetReaderValueGetter(srcType);

            if (valuegetter != null
                && valuegetter.ReturnType == srcType
                && (valuegetter.ReturnType == columnInfo.PropertyInfo.PropertyType ||
                    valuegetter.ReturnType == Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType)))
            {
                castNeeded = false;
            }
            else if ((srcType == columnInfo.PropertyInfo.PropertyType) ||
                     (srcType == Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType)))
            {
                valuegetter = _getValueMethodInfo;
                castNeeded = true;
            }
            else
            {
                throw new InvalidOperationException(
                    Properties.Resources.CannotMaterializeSourceValue.Format(srcType, columnInfo.PropertyInfo.PropertyType));
            }

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            ilGenerator.Emit(OpCodes.Callvirt, valuegetter);

            if (castNeeded)
            {
                EmitCastValue(ilGenerator, columnInfo, srcType);
            }
            else
            {
                if (Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType) != null)
                {
                    ilGenerator.Emit(OpCodes.Newobj,
                        columnInfo.PropertyInfo.PropertyType.GetConstructor(
                            new Type[] { Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType) }));
                }
            }

            ilGenerator.Emit(OpCodes.Callvirt, columnInfo.PropertyInfo.GetSetMethod(true));
        }

        private static void EmitCastValue(ILGenerator ilGenerator, ColumnInfo columnInfo, Type srcType)
        {
            if (srcType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, columnInfo.PropertyInfo.PropertyType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, columnInfo.PropertyInfo.PropertyType);
            }
        }

        private static Dictionary<string, MethodInfo> InitReaderValueGetters()
        {
            var getters = new Dictionary<string, MethodInfo>(StringComparer.CurrentCultureIgnoreCase);

            MethodInfo CreateReaderValueGetter(string typeName)
                => typeof(IDataRecord).GetMethod($"Get{typeName}", new Type[] { typeof(int) });

            void Add<T>()
                => getters.Add(typeof(T).Name, CreateReaderValueGetter(typeof(T).Name));

            Add<Boolean>();
            Add<Byte>();
            Add<Char>();
            Add<DateTime>();
            Add<Decimal>();
            Add<Double>();
            Add<Guid>();
            Add<Int16>();
            Add<Int32>();
            Add<Int64>();

            Add<String>();

            getters.Add(nameof(Single), CreateReaderValueGetter("Float"));

            return getters;
        }

        private static MethodInfo GetReaderValueGetter(Type srcType)
            => _readerValueGetters.ContainsKey(srcType.Name) ? _readerValueGetters[srcType.Name] : null;
    }
}
