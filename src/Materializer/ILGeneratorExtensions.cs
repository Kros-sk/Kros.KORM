using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Kros.KORM.Materializer
{
    internal static class ILGeneratorExtensions
    {
        private readonly static List<IConverter> _converters = new List<IConverter>();
        private readonly static Dictionary<string, MethodInfo> _readerValueGetters = InitReaderValueGetters();
        private readonly static MethodInfo _fnIsDBNull = typeof(IDataRecord).GetMethod(nameof(IDataReader.IsDBNull));
        private readonly static MethodInfo _getValueMethodInfo =
            typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private readonly static FieldInfo _fldConverters = typeof(ILGeneratorExtensions).GetField(nameof(_converters),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static MethodInfo _fnConvertersListGetItem = typeof(List<IConverter>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private readonly static MethodInfo _fnConvert = typeof(IConverter).GetMethod("Convert");
        private readonly static List<IInjector> _injectors = new List<IInjector>();
        private readonly static FieldInfo _fldInjectors = typeof(ILGeneratorExtensions).GetField(nameof(_injectors),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static MethodInfo _fnInjectorsListGetItem = typeof(List<IInjector>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnInjectorMethodInfo =
            typeof(IInjector).GetMethod(nameof(IInjector.GetValue), new Type[] { typeof(string) });

        public static ILGenerator CallReaderMethod(this ILGenerator iLGenerator, int fieldIndex, MethodInfo methodInfo)
        {
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            iLGenerator.Emit(OpCodes.Callvirt, methodInfo);

            return iLGenerator;
        }

        public static Label CallReaderIsDbNull(this ILGenerator iLGenerator, int fieldIndex)
        {
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            iLGenerator.Emit(OpCodes.Callvirt, _fnIsDBNull);
            Label truePart = iLGenerator.DefineLabel();
            iLGenerator.Emit(OpCodes.Brtrue_S, truePart);

            iLGenerator.Emit(OpCodes.Dup);

            return truePart;
        }

        public static MethodInfo GetReaderValueGetter(this Type srcType)
            => _readerValueGetters.ContainsKey(srcType.Name) ? _readerValueGetters[srcType.Name] : null;

        public static void CallReaderGetValueWithoutConverter(
            this ILGenerator iLGenerator,
            int fieldIndex,
            ColumnInfo columnInfo,
            Type srcType)
        {
            MethodInfo valuegetter = srcType.GetReaderValueGetter();

            bool castNeeded;
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

            iLGenerator.CallReaderMethod(fieldIndex, valuegetter);

            if (castNeeded)
            {
                EmitCastValue(iLGenerator, columnInfo, srcType);
            }
            else
            {
                if (Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType) != null)
                {
                    iLGenerator.Emit(OpCodes.Newobj,
                        columnInfo.PropertyInfo.PropertyType.GetConstructor(
                            new Type[] { Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType) }));
                }
            }
        }

        public static void CallReaderGetValueWithConverter(
            this ILGenerator iLGenerator,
            int fieldIndex,
            IConverter converter,
            ColumnInfo columnInfo)
        {
            int converterIndex = _converters.Count;
            _converters.Add(converter);

            iLGenerator.Emit(OpCodes.Ldsfld, _fldConverters);
            iLGenerator.Emit(OpCodes.Ldc_I4, converterIndex);
            iLGenerator.Emit(OpCodes.Callvirt, _fnConvertersListGetItem);

            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            iLGenerator.Emit(OpCodes.Callvirt, _fnGetValue);

            iLGenerator.Emit(OpCodes.Callvirt, _fnConvert);
            iLGenerator.Emit(OpCodes.Unbox_Any, columnInfo.PropertyInfo.PropertyType);
        }

        public static void CallGetInjectedValue(
            this ILGenerator iLGenerator,
            IInjector injector,
            string propertyName,
            Type propertyType)
        {
            int injectorIndex = GetInjectorIndex(injector);

            iLGenerator.Emit(OpCodes.Ldsfld, _fldInjectors);
            iLGenerator.Emit(OpCodes.Ldc_I4, injectorIndex);
            iLGenerator.Emit(OpCodes.Callvirt, _fnInjectorsListGetItem);

            iLGenerator.Emit(OpCodes.Ldstr, propertyName);
            iLGenerator.Emit(OpCodes.Callvirt, _fnInjectorMethodInfo);

            iLGenerator.Emit(OpCodes.Unbox_Any, propertyType);
        }

        public static void CallOnAfterMaterialize(
            this ILGenerator iLGenerator,
            TableInfo tableInfo)
        {
            if (tableInfo.OnAfterMaterialize != null)
            {
                iLGenerator.Emit(OpCodes.Dup);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                if (tableInfo.OnAfterMaterialize.IsVirtual)
                {
                    iLGenerator.Emit(OpCodes.Callvirt, tableInfo.OnAfterMaterialize);
                }
                else
                {
                    iLGenerator.Emit(OpCodes.Call, tableInfo.OnAfterMaterialize);
                }
            }
        }

        private static void EmitCastValue(ILGenerator iLGenerator, ColumnInfo columnInfo, Type srcType)
        {
            if (srcType.IsValueType)
            {
                iLGenerator.Emit(OpCodes.Unbox_Any, columnInfo.PropertyInfo.PropertyType);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Castclass, columnInfo.PropertyInfo.PropertyType);
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
    }
}
