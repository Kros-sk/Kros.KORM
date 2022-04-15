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
    internal static class ILGeneratorHelper
    {
        private readonly static List<IConverter> _converters = new List<IConverter>();
        private readonly static Dictionary<string, MethodInfo> _readerValueGetters = InitReaderValueGetters();
        private readonly static MethodInfo _fnIsDBNull = typeof(IDataRecord).GetMethod(nameof(IDataReader.IsDBNull));
        private readonly static MethodInfo _getValueMethodInfo =
            typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private readonly static FieldInfo _fldConverters = typeof(ILGeneratorHelper).GetField(nameof(_converters),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static MethodInfo _fnConvertersListGetItem = typeof(List<IConverter>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private readonly static MethodInfo _fnConvert = typeof(IConverter).GetMethod("Convert");
        private readonly static List<IInjector> _injectors = new List<IInjector>();
        private readonly static FieldInfo _fldInjectors = typeof(ILGeneratorHelper).GetField(nameof(_injectors),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private readonly static MethodInfo _fnInjectorsListGetItem = typeof(List<IInjector>).GetProperty("Item").GetGetMethod();
        private readonly static MethodInfo _fnInjectorMethodInfo =
            typeof(IInjector).GetMethod(nameof(IInjector.GetValue), new Type[] { typeof(string) });

        public static ILGenerator CallReaderMethod(
            this ILGenerator iLGenerator,
            int fieldIndex,
            MethodInfo methodInfo)
        {
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
            iLGenerator.Emit(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);

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

        public static MethodInfo GetReaderValueGetter(this Type srcType, bool isNullable = false)
        {
            string name = isNullable ? GetNullableName(srcType.Name) : srcType.Name;

            return _readerValueGetters.ContainsKey(name) ? _readerValueGetters[name] : null;
        }

        public static MethodInfo GetReaderValueGetter(ColumnInfo columnInfo, Type srcType, out bool castNeeded)
        {
            MethodInfo valueGetter = srcType.GetReaderValueGetter(columnInfo.IsNullable);

            if (valueGetter != null
                && (valueGetter.ReturnType == columnInfo.PropertyInfo.PropertyType
                || valueGetter.ReturnType == Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType)))
            {
                castNeeded = false;
            }
            else if (valueGetter is null
                && ((srcType == columnInfo.PropertyInfo.PropertyType)
                || (srcType == Nullable.GetUnderlyingType(columnInfo.PropertyInfo.PropertyType))))
            {
                valueGetter = _getValueMethodInfo;
                castNeeded = true;
            }
            else
            {
                throw new InvalidOperationException(
                    Properties.Resources.CannotMaterializeSourceValue.Format(srcType, columnInfo.PropertyInfo.PropertyType));
            }
            return valueGetter;
        }

        public static void CallReaderGetValueWithoutConverter(
            this ILGenerator iLGenerator,
            int fieldIndex,
            ColumnInfo columnInfo,
            Type srcType)
        {
            MethodInfo valueGetter = GetReaderValueGetter(columnInfo, srcType, out bool castNeeded);
            iLGenerator.CallReaderMethod(fieldIndex, valueGetter);
            if (castNeeded)
            {
                EmitCastValue(iLGenerator, srcType, columnInfo.PropertyInfo.PropertyType);
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

        public static void EmitCastValue(ILGenerator iLGenerator, Type srcType, Type targetType)
        {
            if (srcType.IsValueType)
            {
                iLGenerator.Emit(OpCodes.Unbox_Any, targetType);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Castclass, targetType);
            }
        }

        public static void EmitSetNullValue(this ILGenerator ilGenerator, Type propertyType, MethodInfo propertySetter)
        {
            if (propertyType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Callvirt, propertySetter);
            }
            else if (propertyType == typeof(string))
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Callvirt, propertySetter);
            }
        }

        private static Dictionary<string, MethodInfo> InitReaderValueGetters()
        {
            var getters = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);

            MethodInfo CreateReaderValueGetter(string typeName)
                => typeof(IDataRecord).GetMethod($"Get{typeName}", new Type[] { typeof(int) });

            MethodInfo CreateReaderNullableValueGetter(string typeName)
                => typeof(Kros.KORM.Data.DataReaderExtensions)
                    .GetMethod($"GetNullable{typeName}", new Type[] { typeof(IDataReader), typeof(int) });

            void Add<T>()
            {
                string name = typeof(T).Name;
                getters.Add(name, CreateReaderValueGetter(name));
                getters.Add(GetNullableName(name), CreateReaderNullableValueGetter(name));
            }

            Add<bool>();
            Add<byte>();
            Add<char>();
            Add<DateTime>();
            Add<decimal>();
            Add<double>();
            Add<Guid>();
            Add<short>();
            Add<int>();
            Add<long>();

            Add<string>();

            getters.Add(nameof(Single), CreateReaderValueGetter("Float"));
            getters.Add(GetNullableName(nameof(Single)), CreateReaderNullableValueGetter("Float"));

            return getters;
        }

        private static string GetNullableName(string name)
            => $"Nullable{name}";

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
