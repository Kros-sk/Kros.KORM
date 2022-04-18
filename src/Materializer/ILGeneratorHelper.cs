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
            Label falsePart = iLGenerator.DefineLabel();
            iLGenerator.Emit(OpCodes.Brfalse_S, falsePart);

            return falsePart;
        }

        public static MethodInfo GetReaderValueGetter(this Type srcType, bool isNullable = false)
        {
            string name = isNullable ? GetNullableName(srcType.Name) : srcType.Name;
            return _readerValueGetters.ContainsKey(name) ? _readerValueGetters[name] : null;
        }

        public static MethodInfo GetReaderValueGetter(Type propertyType, Type srcType, out bool castNeeded)
        {
            Type nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
            MethodInfo valueGetter = srcType.GetReaderValueGetter(nullableUnderlyingType is not null);

            if (valueGetter != null
                && (valueGetter.ReturnType == propertyType
                || valueGetter.ReturnType == Nullable.GetUnderlyingType(propertyType)))
            {
                castNeeded = false;
            }
            else if (valueGetter is null
                && ((srcType == propertyType)
                || (srcType == nullableUnderlyingType)))
            {
                valueGetter = _getValueMethodInfo;
                castNeeded = true;
            }
            else
            {
                throw new InvalidOperationException(
                    Properties.Resources.CannotMaterializeSourceValue.Format(srcType, propertyType));
            }
            return valueGetter;
        }

        public static void CallReaderGetValueWithoutConverter(
            this ILGenerator iLGenerator,
            int fieldIndex,
            Type propertyType,
            Type srcType)
        {
            MethodInfo valueGetter = GetReaderValueGetter(propertyType, srcType, out bool castNeeded);
            iLGenerator.CallReaderMethod(fieldIndex, valueGetter);
            if (castNeeded)
            {
                EmitCastValue(iLGenerator, srcType, propertyType);
            }
        }

        public static void CallConverter(
            this ILGenerator ilGenerator,
            IConverter converter,
            Type propertyType,
            int fieldIndex,
            bool convertNullValue)
        {
            int converterIndex = _converters.Count;
            _converters.Add(converter);

            ilGenerator.Emit(OpCodes.Ldsfld, _fldConverters);
            ilGenerator.Emit(OpCodes.Ldc_I4, converterIndex);
            ilGenerator.Emit(OpCodes.Callvirt, _fnConvertersListGetItem);

            if (convertNullValue)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4, fieldIndex);
                ilGenerator.Emit(OpCodes.Callvirt, _fnGetValue);
            }

            ilGenerator.Emit(OpCodes.Callvirt, _fnConvert);
            ilGenerator.Emit(OpCodes.Unbox_Any, propertyType);
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

        public static void EmitSetNullValue(this ILGenerator ilGenerator, Type propertyType)
        {
            if (propertyType.IsPrimitive)
            {
                EmitSetNullValueForPrimitiveTypes(ilGenerator, propertyType);
            }
            else if (propertyType.IsValueType)
            {
                EmitSetNullValueForValueTypes(ilGenerator, propertyType);
            }
            else
            {
                // Reference types.
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }

        public static void EmitSetNullValueForPrimitiveTypes(this ILGenerator ilGenerator, Type propertyType)
        {
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
                // Every other primitive type default is just 0.
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }

        public static void EmitSetNullValueForValueTypes(this ILGenerator ilGenerator, Type propertyType)
        {
            LocalBuilder local = ilGenerator.DeclareLocal(propertyType);
            ilGenerator.Emit(OpCodes.Ldloca_S, local.LocalIndex);
            ilGenerator.Emit(OpCodes.Initobj, local.LocalType);
            ilGenerator.Emit(OpCodes.Ldloc_S, local.LocalIndex);
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
