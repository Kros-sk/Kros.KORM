using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Kros.KORM.Materializer
{
    /// <summary>
    /// Model factory for record types.
    /// </summary>
    internal static class RecordModelFactory
    {
        public static Func<IDataReader, T> CreateFactoryForRecords<T>(
           IDataReader reader,
           TableInfo tableInfo,
           IInjector injector,
           ConstructorInfo ctor)
        {
            Type type = typeof(T);
            var dynamicMethod = new DynamicMethod(
                $"korm_factory_record_{typeof(T).Name}",
                type, new Type[] { typeof(IDataReader) }, true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            foreach (ParameterInfo param in paramsInfo)
            {
                if (injector.IsInjectable(param.Name))
                {
                    ilGenerator.CallGetInjectedValue(injector, param.Name, param.ParameterType);
                }
                else
                {
                    ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(param.Name);
                    if (columnInfo is null)
                    {
                        throw new InvalidOperationException(
                            Properties.Resources.ConstructorParameterDoesNotMatchProperty.Format(param.Name, type.FullName));
                    }
                    FromReader(reader, ilGenerator, columnInfo);
                }
            }

            ilGenerator.DeclareLocal(typeof(T));
            ilGenerator.Emit(OpCodes.Newobj, ctor);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.CallOnAfterMaterialize(tableInfo);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
        }

        private static void FromReader(IDataReader reader, ILGenerator iLGenerator, ColumnInfo columnInfo)
        {
            int ordinal = reader.GetOrdinal(columnInfo.Name);
            Type srcType = reader.GetFieldType(ordinal);

            IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);
            if (converter is null)
            {
                iLGenerator.CallReaderGetValueWithoutConverter(ordinal, columnInfo.PropertyInfo.PropertyType, srcType);
            }
            else
            {
                iLGenerator.CallConverter(converter, columnInfo.PropertyInfo.PropertyType, ordinal, convertNullValue: false);
            }
        }
    }
}
