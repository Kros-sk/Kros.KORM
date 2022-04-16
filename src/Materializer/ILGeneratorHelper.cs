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
        #region Nested types

        internal static class NullablePrimitives
        {
            private static readonly Dictionary<Type, FieldInfo> _fields = new()
            {
                { typeof(bool), typeof(NullablePrimitives).GetField(nameof(NullableBool)) },
                { typeof(byte), typeof(NullablePrimitives).GetField(nameof(NullableByte)) },
                { typeof(sbyte), typeof(NullablePrimitives).GetField(nameof(NullableSByte)) },
                { typeof(short), typeof(NullablePrimitives).GetField(nameof(NullableInt16)) },
                { typeof(ushort), typeof(NullablePrimitives).GetField(nameof(NullableUInt16)) },
                { typeof(int), typeof(NullablePrimitives).GetField(nameof(NullableInt32)) },
                { typeof(uint), typeof(NullablePrimitives).GetField(nameof(NullableUInt32)) },
                { typeof(long), typeof(NullablePrimitives).GetField(nameof(NullableInt64)) },
                { typeof(ulong), typeof(NullablePrimitives).GetField(nameof(NullableUInt64)) },
                { typeof(char), typeof(NullablePrimitives).GetField(nameof(NullableChar)) },
                { typeof(double), typeof(NullablePrimitives).GetField(nameof(NullableDouble)) },
                { typeof(float), typeof(NullablePrimitives).GetField(nameof(NullableSingle)) }
            };

            public static FieldInfo GetFieldInfo(Type type) => _fields[type];

            public static readonly bool? NullableBool = null;
            public static readonly byte? NullableByte = null;
            public static readonly sbyte? NullableSByte = null;
            public static readonly short? NullableInt16 = null;
            public static readonly ushort? NullableUInt16 = null;
            public static readonly int? NullableInt32 = null;
            public static readonly uint? NullableUInt32 = null;
            public static readonly long? NullableInt64 = null;
            public static readonly ulong? NullableUInt64 = null;
            public static readonly char? NullableChar = null;
            public static readonly double? NullableDouble = null;
            public static readonly float? NullableSingle = null;
        }

        #endregion

        private static readonly List<IConverter> _converters = new List<IConverter>();
        private static readonly Dictionary<string, MethodInfo> _readerValueGetters = InitReaderValueGetters();
        private static readonly MethodInfo _fnIsDBNull = typeof(IDataRecord).GetMethod(nameof(IDataReader.IsDBNull));
        private static readonly MethodInfo _getValueMethodInfo =
            typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private static readonly FieldInfo _fldConverters = typeof(ILGeneratorHelper).GetField(nameof(_converters),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private static readonly MethodInfo _fnConvertersListGetItem = typeof(List<IConverter>).GetProperty("Item").GetGetMethod();
        private static readonly MethodInfo _fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private static readonly MethodInfo _fnConvert = typeof(IConverter).GetMethod("Convert");
        private static readonly List<IInjector> _injectors = new List<IInjector>();
        private static readonly FieldInfo _fldInjectors = typeof(ILGeneratorHelper).GetField(nameof(_injectors),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private static readonly MethodInfo _fnInjectorsListGetItem = typeof(List<IInjector>).GetProperty("Item").GetGetMethod();
        private static readonly MethodInfo _fnInjectorMethodInfo =
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

        private static void EmitCastValue(ILGenerator iLGenerator, Type srcType, Type targetType)
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
            if (propertyType.IsPrimitive)
            {
                EmitSetNullValueForPrimitiveTypes(ilGenerator, propertyType, propertySetter);
            }
            else if (propertyType.IsValueType)
            {
                Type nullableType = Nullable.GetUnderlyingType(propertyType);
                if (nullableType != null)
                {
                    if (nullableType.IsPrimitive)
                    {
                        EmitSetNullValueForNullablePrimitiveTypes(ilGenerator, nullableType, propertySetter);
                    }
                }
                else
                {
                }
            }
            else
            {
                // Reference types.
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Callvirt, propertySetter);
            }
        }

        public static void EmitSetNullValueForPrimitiveTypes(this ILGenerator ilGenerator, Type propertyType, MethodInfo propertySetter)
        {
            ilGenerator.Emit(OpCodes.Ldloc_0);
            if ((propertyType == typeof(long)) || (propertyType == typeof(ulong)))
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Conv_I8);
            }
            else if (propertyType == typeof(double))
            {
                ilGenerator.Emit(OpCodes.Ldc_R8, (double)default);
            }
            else if (propertyType == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Ldc_R4, (float)default);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            ilGenerator.Emit(OpCodes.Callvirt, propertySetter);
        }

        public static void EmitSetNullValueForNullablePrimitiveTypes(this ILGenerator ilGenerator, Type propertyType, MethodInfo propertySetter)
        {
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldsfld, NullablePrimitives.GetFieldInfo(propertyType));
            ilGenerator.Emit(OpCodes.Callvirt, propertySetter);
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
