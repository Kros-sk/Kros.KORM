using Kros.KORM.Properties;
using System;
using System.Collections.Generic;
using System.Data;

namespace Kros.KORM.Extensions
{
    /// <summary>
    /// .NET clr type extensions.
    /// </summary>
    internal static class TypeExtensions
    {
        private static readonly Dictionary<Type, DbType> _dbTypeMap = new()
        {
            { typeof(bool), DbType.Boolean },
            { typeof(bool?), DbType.Boolean },
            { typeof(byte[]), DbType.Binary },
            { typeof(byte), DbType.Byte },
            { typeof(byte?), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(sbyte?), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(short?), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(ushort?), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(int?), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(uint?), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(long?), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(ulong?), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(float?), DbType.Single },
            { typeof(decimal), DbType.Decimal },
            { typeof(decimal?), DbType.Decimal },
            { typeof(double), DbType.Double },
            { typeof(double?), DbType.Double },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTime?), DbType.DateTime },
            { typeof(Guid), DbType.Guid },
            { typeof(object), DbType.Binary },
            { typeof(string), DbType.String }
        };

        private static readonly Dictionary<Type, string> _sqlTypeMap = new()
        {
            { typeof(bool), "bit" },
            { typeof(bool?), "bit" },
            { typeof(byte[]), "varBinary" },
            { typeof(byte), "tinyInt" },
            { typeof(byte?), "tinyInt" },
            { typeof(sbyte), "tinyInt" },
            { typeof(sbyte?), "tinyInt" },
            { typeof(short), "smallInt" },
            { typeof(short?), "smallInt" },
            { typeof(ushort), "smallInt" },
            { typeof(ushort?), "smallInt" },
            { typeof(int), "int" },
            { typeof(int?), "int" },
            { typeof(uint), "int" },
            { typeof(uint?), "int" },
            { typeof(long), "bigInt" },
            { typeof(long?), "bigInt" },
            { typeof(ulong), "bigInt" },
            { typeof(ulong?), "bigInt" },
            { typeof(float), "real" },
            { typeof(float?), "real" },
            { typeof(decimal), "decimal" },
            { typeof(decimal?), "decimal" },
            { typeof(double), "float" },
            { typeof(double?), "float" },
            { typeof(DateTime), "dateTime" },
            { typeof(DateTime?), "dateTime" },
            { typeof(Guid), "uniqueIdentifier" },
            { typeof(object), "varBinary" },
            { typeof(string), "nVarChar(255)" }
        };

        /// <summary>
        /// Converts .NET clr type to DbType.
        /// </summary>
        /// <param name="type">Clr type.</param>
        /// <returns>DbType.</returns>
        /// <exception cref="NotSupportedException">When clr type is not supported.</exception>
        public static DbType ToDbType(this Type type)
        {
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            return _dbTypeMap.TryGetValue(type, out DbType dbType)
                ? dbType
                : throw new NotSupportedException(string.Format(Resources.ConversionFromTypeIsNotSupported, type.Name));
        }

        /// <summary>
        /// Converts .NET clr type to SQL data type.
        /// </summary>
        /// <param name="type">Clr type.</param>
        /// <returns>SQL data type.</returns>
        /// <exception cref="NotSupportedException">When clr type is not supported.</exception>
        public static string ToSqlDataType(this Type type)
        {
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            return _sqlTypeMap.TryGetValue(type, out string sqlDataType)
                ? sqlDataType
                : throw new NotSupportedException(string.Format(Resources.ConversionFromTypeIsNotSupported, type.Name));
        }
    }
}
