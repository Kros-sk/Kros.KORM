using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Kros.KORM.Materializer
{
    internal static class ILGeneratorHelper
    {
        private static readonly object _convertersLock = new();
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
        private static readonly object _injectorsLock = new();
        private static readonly List<IInjector> _injectors = new List<IInjector>();
        private static readonly FieldInfo _fldInjectors = typeof(ILGeneratorHelper).GetField(nameof(_injectors),
            BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
        private static readonly MethodInfo _fnInjectorsListGetItem = typeof(List<IInjector>).GetProperty("Item").GetGetMethod();
        private static readonly MethodInfo _fnInjectorMethodInfo =
            typeof(IInjector).GetMethod(nameof(IInjector.GetValue), new Type[] { typeof(string) });

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, ILogger logger)
        {
            logger.LogTrace("{opCode}", opCode);
            ilGenerator.Emit(opCode);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, ConstructorInfo ctor, ILogger logger)
        {
            logger.LogTrace("{opCode} {type}", opCode, ctor.DeclaringType.FullName);
            ilGenerator.Emit(opCode, ctor);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, MethodInfo method, ILogger logger)
        {
            logger.LogTrace("{opCode} {type}.{methodName}", opCode, method.DeclaringType.FullName, method.Name);
            ilGenerator.Emit(opCode, method);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, FieldInfo field, ILogger logger)
        {
            logger.LogTrace("{opCode} {type}.{fieldName}", opCode, field.DeclaringType.FullName, field.Name);
            ilGenerator.Emit(opCode, field);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, Type type, ILogger logger)
        {
            logger.LogTrace("{opCode} {type}", opCode, type.FullName);
            ilGenerator.Emit(opCode, type);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, Label label, ILogger logger)
        {
            logger.LogTrace("{opCode} label", opCode);
            ilGenerator.Emit(opCode, label);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, string arg, ILogger logger)
        {
            logger.LogTrace("{opCode} {arg}", opCode, arg);
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, int arg, ILogger logger)
        {
            logger.LogTrace("{opCode} {arg}", opCode, arg);
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, double arg, ILogger logger)
        {
            logger.LogTrace("{opCode} {arg}", opCode, arg);
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, float arg, ILogger logger)
        {
            logger.LogTrace("{opCode} {arg}", opCode, arg);
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static void EmitFieldWithoutConverter(
            this ILGenerator ilGenerator,
            Type srcType,
            Type propertyType,
            int columnIndex,
            ILogger logger)
        {
            // Emit: if (reader.IsDbNull(columnIndex)) {
            Label labelIsNotDbNull = ilGenerator.CallReaderIsDbNull(columnIndex, logger);
            Label labelEnd = ilGenerator.DefineLabel();
            ilGenerator.EmitSetNullValue(propertyType, logger);
            ilGenerator.LogAndEmit(OpCodes.Br_S, labelEnd, logger);

            // Emit: } else {
            ilGenerator.MarkLabel(labelIsNotDbNull);
            ilGenerator.CallReaderGetValueWithoutConverter(columnIndex, propertyType, srcType, logger);

            // Emit: }
            ilGenerator.MarkLabel(labelEnd);
        }

        public static void EmitFieldWithConverter(
            this ILGenerator ilGenerator,
            IConverter converter,
            Type propertyType,
            int columnIndex,
            ILogger logger)
        {
            // Emit: if (reader.IsDbNull(columnIndex)) {
            Label labelIsNotDbNull = ilGenerator.CallReaderIsDbNull(columnIndex, logger);
            Label labelEnd = ilGenerator.DefineLabel();
            ilGenerator.CallConverter(converter, propertyType, columnIndex, convertNullValue: true, logger);
            ilGenerator.LogAndEmit(OpCodes.Br_S, labelEnd, logger);

            // Emit: } else {
            ilGenerator.MarkLabel(labelIsNotDbNull);
            ilGenerator.CallConverter(converter, propertyType, columnIndex, convertNullValue: false, logger);

            // Emit: }
            ilGenerator.MarkLabel(labelEnd);
        }

        private static ILGenerator CallReaderMethod(
            this ILGenerator ilGenerator,
            int fieldIndex,
            MethodInfo methodInfo,
            ILogger logger)
        {
            ilGenerator.LogAndEmit(OpCodes.Ldarg_0, logger);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex, logger);
            ilGenerator.LogAndEmit(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo, logger);

            return ilGenerator;
        }

        private static Label CallReaderIsDbNull(this ILGenerator ilGenerator, int fieldIndex, ILogger logger)
        {
            ilGenerator.LogAndEmit(OpCodes.Ldarg_0, logger);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex, logger);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnIsDBNull, logger);
            Label falsePart = ilGenerator.DefineLabel();
            ilGenerator.LogAndEmit(OpCodes.Brfalse_S, falsePart, logger);

            return falsePart;
        }

        public static MethodInfo GetReaderValueGetter(this Type srcType, bool isNullable = false)
        {
            string name = isNullable ? GetNullableName(srcType.Name) : srcType.Name;
            return _readerValueGetters.ContainsKey(name) ? _readerValueGetters[name] : null;
        }

        private static MethodInfo GetReaderValueGetter(Type propertyType, Type srcType, out bool castNeeded)
        {
            Type nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
            MethodInfo valueGetter = srcType.GetReaderValueGetter(nullableUnderlyingType is not null);

            if (valueGetter != null
                && (valueGetter.ReturnType == propertyType
                || valueGetter.ReturnType == nullableUnderlyingType))
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

        private static void CallReaderGetValueWithoutConverter(
            this ILGenerator ilGenerator,
            int fieldIndex,
            Type propertyType,
            Type srcType,
            ILogger logger)
        {
            MethodInfo valueGetter = GetReaderValueGetter(propertyType, srcType, out bool castNeeded);
            ilGenerator.CallReaderMethod(fieldIndex, valueGetter, logger);
            if (castNeeded)
            {
                EmitCastValue(ilGenerator, srcType, propertyType, logger);
            }
        }

        private static void CallConverter(
            this ILGenerator ilGenerator,
            IConverter converter,
            Type propertyType,
            int fieldIndex,
            bool convertNullValue,
            ILogger logger)
        {
            int converterIndex;
            lock (_convertersLock)
            {
                converterIndex = _converters.Count;
                _converters.Add(converter);
            }

            ilGenerator.LogAndEmit(OpCodes.Ldsfld, _fldConverters, logger);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, converterIndex, logger);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnConvertersListGetItem, logger);

            if (convertNullValue)
            {
                ilGenerator.LogAndEmit(OpCodes.Ldnull, logger);
            }
            else
            {
                // Convert value from data reader.
                ilGenerator.LogAndEmit(OpCodes.Ldarg_0, logger);
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex, logger);
                ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnGetValue, logger);
            }

            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnConvert, logger);
            ilGenerator.LogAndEmit(OpCodes.Unbox_Any, propertyType, logger);
        }

        public static void CallGetInjectedValue(
            this ILGenerator ilGenerator,
            IInjector injector,
            string propertyName,
            Type propertyType,
            ILogger logger)
        {
            int injectorIndex = GetInjectorIndex(injector);

            ilGenerator.LogAndEmit(OpCodes.Ldsfld, _fldInjectors, logger);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, injectorIndex, logger);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnInjectorsListGetItem, logger);

            ilGenerator.LogAndEmit(OpCodes.Ldstr, propertyName, logger);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnInjectorMethodInfo, logger);

            ilGenerator.LogAndEmit(OpCodes.Unbox_Any, propertyType, logger);
        }

        public static void CallOnAfterMaterialize(
            this ILGenerator ilGenerator,
            TableInfo tableInfo,
            ILogger logger)
        {
            if (tableInfo.OnAfterMaterialize != null)
            {
                logger.LogDebug("Emitting OnAfterMaterialize call: {typeName}.{methodName}",
                    tableInfo.OnAfterMaterialize.DeclaringType.FullName, tableInfo.OnAfterMaterialize.Name);
                ilGenerator.LogAndEmit(OpCodes.Ldloc_0, logger);
                ilGenerator.LogAndEmit(OpCodes.Ldarg_0, logger);
                if (tableInfo.OnAfterMaterialize.IsVirtual)
                {
                    ilGenerator.LogAndEmit(OpCodes.Callvirt, tableInfo.OnAfterMaterialize, logger);
                }
                else
                {
                    ilGenerator.LogAndEmit(OpCodes.Call, tableInfo.OnAfterMaterialize, logger);
                }
            }
        }

        private static void EmitCastValue(ILGenerator ilGenerator, Type srcType, Type targetType, ILogger logger)
        {
            if (srcType.IsValueType)
            {
                ilGenerator.LogAndEmit(OpCodes.Unbox_Any, targetType, logger);
            }
            else
            {
                ilGenerator.LogAndEmit(OpCodes.Castclass, targetType, logger);
            }
        }

        private static void EmitSetNullValue(this ILGenerator ilGenerator, Type propertyType, ILogger logger)
        {
            if (propertyType.IsPrimitive)
            {
                EmitSetDefaultValueForPrimitiveTypes(ilGenerator, propertyType, logger);
            }
            else if (propertyType.IsValueType)
            {
                EmitSetDefaultValueForValueTypes(ilGenerator, propertyType, logger);
            }
            else
            {
                // Reference types.
                ilGenerator.LogAndEmit(OpCodes.Ldnull, logger);
            }
        }

        private static void EmitSetDefaultValueForPrimitiveTypes(this ILGenerator ilGenerator, Type propertyType, ILogger logger)
        {
            if ((propertyType == typeof(long)) || (propertyType == typeof(ulong)))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4_0, logger);
                ilGenerator.LogAndEmit(OpCodes.Conv_I8, logger);
            }
            else if (propertyType == typeof(double))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_R8, (double)default, logger);
            }
            else if (propertyType == typeof(float))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_R4, (float)default, logger);
            }
            else
            {
                // Every other primitive type default is just 0.
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4_0, logger);
            }
        }

        private static readonly FieldInfo _zeroDecimal = typeof(decimal).GetField(nameof(decimal.Zero));

        private static void EmitSetDefaultValueForValueTypes(this ILGenerator ilGenerator, Type propertyType, ILogger logger)
        {
            if (propertyType == typeof(decimal))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldsfld, _zeroDecimal, logger);
            }
            else if (propertyType.IsEnum)
            {
                ilGenerator.EmitSetDefaultValueForPrimitiveTypes(propertyType.GetEnumUnderlyingType(), logger);
            }
            else
            {
                LocalBuilder local = ilGenerator.DeclareLocal(propertyType);
                ilGenerator.LogAndEmit(OpCodes.Ldloca_S, local.LocalIndex, logger);
                ilGenerator.LogAndEmit(OpCodes.Initobj, local.LocalType, logger);
                ilGenerator.LogAndEmit(OpCodes.Ldloc, local.LocalIndex, logger);
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
                lock (_injectorsLock)
                {
                    _injectors.Add(injector);
                    injectorIndex = _injectors.Count - 1;
                }
            }

            return injectorIndex;
        }
    }
}
