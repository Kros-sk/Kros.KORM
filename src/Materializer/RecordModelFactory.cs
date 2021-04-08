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
                ColumnInfo columnInfo = tableInfo.GetColumnInfoByPropertyName(param.Name);
                if (injector.IsInjectable(param.Name))
                {
                    iLGenerator.CallGetInjectedValue(injector, param.Name, param.ParameterType);
                }
                else
                {
                    string fieldName = columnInfo.Name;
                    int ordinal = reader.GetOrdinal(fieldName);

                    Type srcType = reader.GetFieldType(ordinal);

                    IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);

                    if (converter == null)
                    {
                        iLGenerator.CallReaderGetValueWithoutConverter(ordinal, columnInfo, srcType);
                    }
                    else
                    {
                        iLGenerator.CallReaderGetValueWithConverter(ordinal, converter, columnInfo);
                    }
                }
            }

            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.CallOnAfterMaterialize(tableInfo);
            iLGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
        }
    }
}
