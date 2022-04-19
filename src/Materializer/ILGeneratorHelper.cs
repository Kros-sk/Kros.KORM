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


        [ThreadStatic]
        public static Action<string> Logger;

        private static void Log(string msg)
        {
            if (Logger is not null)
            {
                Logger(msg);
            }
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode)
        {
            Log(opCode.ToString());
            ilGenerator.Emit(opCode);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, ConstructorInfo ctor)
        {
            Log($"{opCode} {ctor.DeclaringType.FullName}");
            ilGenerator.Emit(opCode, ctor);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, MethodInfo method)
        {
            Log($"{opCode} {method.DeclaringType.FullName}.{method.Name}");
            ilGenerator.Emit(opCode, method);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, FieldInfo field)
        {
            Log($"{opCode} {field.DeclaringType.FullName}.{field.Name}");
            ilGenerator.Emit(opCode, field);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, Type type)
        {
            Log($"{opCode} {type.FullName}");
            ilGenerator.Emit(opCode, type);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, Label label)
        {
            Log($"{opCode} label");
            ilGenerator.Emit(opCode, label);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, string arg)
        {
            Log($"{opCode} {arg}");
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, int arg)
        {
            Log($"{opCode} {arg}");
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, double arg)
        {
            Log($"{opCode} {arg}");
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static ILGenerator LogAndEmit(this ILGenerator ilGenerator, OpCode opCode, float arg)
        {
            Log($"{opCode} {arg}");
            ilGenerator.Emit(opCode, arg);
            return ilGenerator;
        }

        public static void EmitFieldWithoutConverter(
            this ILGenerator ilGenerator,
            Type srcType,
            Type propertyType,
            int columnIndex)
        {
            // if (reader.IsDbNull(columnIndex)) {
            Label labelIsNotDbNull = ilGenerator.CallReaderIsDbNull(columnIndex);
            Label labelEnd = ilGenerator.DefineLabel();
            ilGenerator.EmitSetNullValue(propertyType);
            ilGenerator.LogAndEmit(OpCodes.Br_S, labelEnd);

            // } else {
            ilGenerator.MarkLabel(labelIsNotDbNull);
            ilGenerator.CallReaderGetValueWithoutConverter(columnIndex, propertyType, srcType);

            // }
            ilGenerator.MarkLabel(labelEnd);
        }

        public static ILGenerator CallReaderMethod(
            this ILGenerator ilGenerator,
            int fieldIndex,
            MethodInfo methodInfo)
        {
            ilGenerator.LogAndEmit(OpCodes.Ldarg_0);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex);
            ilGenerator.LogAndEmit(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);

            return ilGenerator;
        }

        public static Label CallReaderIsDbNull(this ILGenerator ilGenerator, int fieldIndex)
        {
            ilGenerator.LogAndEmit(OpCodes.Ldarg_0);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnIsDBNull);
            Label falsePart = ilGenerator.DefineLabel();
            ilGenerator.LogAndEmit(OpCodes.Brfalse_S, falsePart);

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
            Type srcType)
        {
            MethodInfo valueGetter = GetReaderValueGetter(propertyType, srcType, out bool castNeeded);
            ilGenerator.CallReaderMethod(fieldIndex, valueGetter);
            if (castNeeded)
            {
                EmitCastValue(ilGenerator, srcType, propertyType);
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

            ilGenerator.LogAndEmit(OpCodes.Ldsfld, _fldConverters);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, converterIndex);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnConvertersListGetItem);

            if (convertNullValue)
            {
                ilGenerator.LogAndEmit(OpCodes.Ldnull);
            }
            else
            {
                // Convert value from data reader.
                ilGenerator.LogAndEmit(OpCodes.Ldarg_0);
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4, fieldIndex);
                ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnGetValue);
            }

            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnConvert);
            ilGenerator.LogAndEmit(OpCodes.Unbox_Any, propertyType);
        }

        public static void CallGetInjectedValue(
            this ILGenerator ilGenerator,
            IInjector injector,
            string propertyName,
            Type propertyType)
        {
            int injectorIndex = GetInjectorIndex(injector);

            ilGenerator.LogAndEmit(OpCodes.Ldsfld, _fldInjectors);
            ilGenerator.LogAndEmit(OpCodes.Ldc_I4, injectorIndex);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnInjectorsListGetItem);

            ilGenerator.LogAndEmit(OpCodes.Ldstr, propertyName);
            ilGenerator.LogAndEmit(OpCodes.Callvirt, _fnInjectorMethodInfo);

            ilGenerator.LogAndEmit(OpCodes.Unbox_Any, propertyType);
        }

        public static void CallOnAfterMaterialize(
            this ILGenerator ilGenerator,
            TableInfo tableInfo)
        {
            if (tableInfo.OnAfterMaterialize != null)
            {
                ilGenerator.LogAndEmit(OpCodes.Ldloc_0);
                ilGenerator.LogAndEmit(OpCodes.Ldarg_0);
                if (tableInfo.OnAfterMaterialize.IsVirtual)
                {
                    ilGenerator.LogAndEmit(OpCodes.Callvirt, tableInfo.OnAfterMaterialize);
                }
                else
                {
                    ilGenerator.LogAndEmit(OpCodes.Call, tableInfo.OnAfterMaterialize);
                }
            }
        }

        private static void EmitCastValue(ILGenerator ilGenerator, Type srcType, Type targetType)
        {
            if (srcType.IsValueType)
            {
                ilGenerator.LogAndEmit(OpCodes.Unbox_Any, targetType);
            }
            else
            {
                ilGenerator.LogAndEmit(OpCodes.Castclass, targetType);
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
                ilGenerator.LogAndEmit(OpCodes.Ldnull);
            }
        }

        public static void EmitSetNullValueForPrimitiveTypes(this ILGenerator ilGenerator, Type propertyType)
        {
            if ((propertyType == typeof(long)) || (propertyType == typeof(ulong)))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4_0);
                ilGenerator.LogAndEmit(OpCodes.Conv_I8);
            }
            else if (propertyType == typeof(double))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_R8, (double)default);
            }
            else if (propertyType == typeof(float))
            {
                ilGenerator.LogAndEmit(OpCodes.Ldc_R4, (float)default);
            }
            else
            {
                // Every other primitive type default is just 0.
                ilGenerator.LogAndEmit(OpCodes.Ldc_I4_0);
            }
        }

        public static void EmitSetNullValueForValueTypes(this ILGenerator ilGenerator, Type propertyType)
        {
            LocalBuilder local = ilGenerator.DeclareLocal(propertyType);
            ilGenerator.LogAndEmit(OpCodes.Ldloca_S, local.LocalIndex);
            ilGenerator.LogAndEmit(OpCodes.Initobj, local.LocalType);
            ilGenerator.LogAndEmit(OpCodes.Ldloc_S, local.LocalIndex);
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
