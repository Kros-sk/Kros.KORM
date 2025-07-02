using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Metadata;
using Microsoft.Extensions.Logging;
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
    internal class RecordModelFactory
    {
        private readonly ILogger<RecordModelFactory> _logger;

        public RecordModelFactory()
        {
            _logger = KormLogging.CreateLogger<RecordModelFactory>();
        }

        public Func<IDataReader, T> CreateFactoryForRecords<T>(
           IDataReader reader,
           TableInfo tableInfo,
           IInjector injector,
           ConstructorInfo ctor)
        {
            Type type = typeof(T);
            string factoryName = $"korm_factory_record_{typeof(T).Name}";
            var dynamicMethod = new DynamicMethod(
                factoryName,
                type, new Type[] { typeof(IDataReader) }, true);
            _logger.LogDebug("Start creating dynamic factory method '{factoryName}'.", factoryName);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            foreach (ParameterInfo param in paramsInfo)
            {
                _logger.LogDebug("Emitting constructor parameter '{paramName}' of type '{paramType}'.",
                    param.Name, param.ParameterType.FullName);
                if (injector.IsInjectable(param.Name))
                {
                    _logger.LogDebug("  Parameter is injectable, calling injector.");
                    ilGenerator.CallGetInjectedValue(injector, param.Name, param.ParameterType, _logger);
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

            LocalBuilder localResult = ilGenerator.DeclareLocal(typeof(T));
            ilGenerator.LogAndEmit(OpCodes.Newobj, ctor, _logger);
            ilGenerator.LogAndEmit(OpCodes.Stloc_S, localResult.LocalIndex, _logger);
            ilGenerator.CallOnAfterMaterialize(tableInfo, _logger);
            ilGenerator.LogAndEmit(OpCodes.Ldloc, localResult.LocalIndex, _logger);
            ilGenerator.LogAndEmit(OpCodes.Ret, _logger);

            _logger.LogDebug("End creating dynamic factory method '{factoryName}'.", factoryName);
            return dynamicMethod.CreateDelegate(Expression.GetFuncType(typeof(IDataReader), type)) as Func<IDataReader, T>;
        }

        private void FromReader(IDataReader reader, ILGenerator ilGenerator, ColumnInfo columnInfo)
        {
            int ordinal = reader.GetOrdinal(columnInfo.Name);
            Type srcType = reader.GetFieldType(ordinal);

            _logger.LogDebug("Emitting field {fieldIndex} from data reader: {fieldName}.", ordinal, columnInfo.Name);
            _logger.LogDebug("  Field type is {type}.", srcType.FullName);
            IConverter converter = ConverterHelper.GetConverter(columnInfo, srcType);
            if (converter is null)
            {
                _logger.LogDebug("  Field does not have a converter.");
                ilGenerator.EmitFieldWithoutConverter(srcType, columnInfo.PropertyInfo.PropertyType, ordinal, _logger);
            }
            else
            {
                _logger.LogDebug("  Field has a converter.");
                ilGenerator.EmitFieldWithConverter(converter, columnInfo.PropertyInfo.PropertyType, ordinal, _logger);
            }
        }
    }
}
