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
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            foreach (ParameterInfo param in paramsInfo)
            {
                if (injector.IsInjectable(param.Name))
                {
                    iLGenerator.CallGetInjectedValue(injector, param.Name, param.ParameterType);
                }
                else
                {
                    ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(param.Name);
                    if (columnInfo is null)
                    {
                        throw new InvalidOperationException(
                            Properties.Resources.ConstructorParameterDoesNotMatchProperty.Format(param.Name, type.FullName));
                    }
                    FromReader(reader, iLGenerator, columnInfo);
                }
            }

            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.CallOnAfterMaterialize(tableInfo);
            iLGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
        }

        private static void FromReader(IDataReader reader, ILGenerator iLGenerator, ColumnInfo columnInfo)
        {
            string fieldName = columnInfo.Name;
            int ordinal = reader.GetOrdinal(fieldName);

            Type srcType = reader.GetFieldType(ordinal);

            IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);

            if (converter is null)
            {
                iLGenerator.CallReaderGetValueWithoutConverter(ordinal, columnInfo, srcType);
            }
            else
            {
                iLGenerator.CallReaderGetValueWithConverter(ordinal, converter, columnInfo);
            }
        }
    }
}
