using FluentAssertions;
using Kros.KORM.Extensions;
using System;
using System.Data;
using Xunit;

namespace Kros.KORM.UnitTests.Extensions
{
    public class TypeExtensionsShould
    {
        [Fact]
        public void ConvertToDbType()
        {
            typeof(bool).ToDbType().Should().Be(DbType.Boolean);
            typeof(bool?).ToDbType().Should().Be(DbType.Boolean);
            typeof(byte[]).ToDbType().Should().Be(DbType.Binary);
            typeof(byte).ToDbType().Should().Be(DbType.Byte);
            typeof(byte?).ToDbType().Should().Be(DbType.Byte);
            typeof(sbyte).ToDbType().Should().Be(DbType.SByte);
            typeof(sbyte?).ToDbType().Should().Be(DbType.SByte);
            typeof(short).ToDbType().Should().Be(DbType.Int16);
            typeof(short?).ToDbType().Should().Be(DbType.Int16);
            typeof(ushort).ToDbType().Should().Be(DbType.UInt16);
            typeof(ushort?).ToDbType().Should().Be(DbType.UInt16);
            typeof(int).ToDbType().Should().Be(DbType.Int32);
            typeof(int?).ToDbType().Should().Be(DbType.Int32);
            typeof(uint).ToDbType().Should().Be(DbType.UInt32);
            typeof(uint?).ToDbType().Should().Be(DbType.UInt32);
            typeof(long).ToDbType().Should().Be(DbType.Int64);
            typeof(long?).ToDbType().Should().Be(DbType.Int64);
            typeof(ulong).ToDbType().Should().Be(DbType.UInt64);
            typeof(ulong?).ToDbType().Should().Be(DbType.UInt64);
            typeof(float).ToDbType().Should().Be(DbType.Single);
            typeof(float?).ToDbType().Should().Be(DbType.Single);
            typeof(decimal).ToDbType().Should().Be(DbType.Decimal);
            typeof(decimal?).ToDbType().Should().Be(DbType.Decimal);
            typeof(double).ToDbType().Should().Be(DbType.Double);
            typeof(double?).ToDbType().Should().Be(DbType.Double);
            typeof(DateTime).ToDbType().Should().Be(DbType.DateTime);
            typeof(DateTime?).ToDbType().Should().Be(DbType.DateTime);
            typeof(Guid).ToDbType().Should().Be(DbType.Guid);
            typeof(object).ToDbType().Should().Be(DbType.Binary);
            typeof(string).ToDbType().Should().Be(DbType.String);
        }

        [Fact]
        public void ConvertToSqlDataType()
        {
            typeof(bool).ToSqlDataType().Should().Be("bit");
            typeof(bool?).ToSqlDataType().Should().Be("bit");
            typeof(byte[]).ToSqlDataType().Should().Be("varBinary");
            typeof(byte).ToSqlDataType().Should().Be("tinyInt");
            typeof(byte?).ToSqlDataType().Should().Be("tinyInt");
            typeof(sbyte).ToSqlDataType().Should().Be("tinyInt");
            typeof(sbyte?).ToSqlDataType().Should().Be("tinyInt");
            typeof(short).ToSqlDataType().Should().Be("smallInt");
            typeof(short?).ToSqlDataType().Should().Be("smallInt");
            typeof(ushort).ToSqlDataType().Should().Be("smallInt");
            typeof(ushort?).ToSqlDataType().Should().Be("smallInt");
            typeof(int).ToSqlDataType().Should().Be("int");
            typeof(int?).ToSqlDataType().Should().Be("int");
            typeof(uint).ToSqlDataType().Should().Be("int");
            typeof(uint?).ToSqlDataType().Should().Be("int");
            typeof(long).ToSqlDataType().Should().Be("bigInt");
            typeof(long?).ToSqlDataType().Should().Be("bigInt");
            typeof(ulong).ToSqlDataType().Should().Be("bigInt");
            typeof(ulong?).ToSqlDataType().Should().Be("bigInt");
            typeof(float).ToSqlDataType().Should().Be("real");
            typeof(float?).ToSqlDataType().Should().Be("real");
            typeof(decimal).ToSqlDataType().Should().Be("decimal");
            typeof(decimal?).ToSqlDataType().Should().Be("decimal");
            typeof(double).ToSqlDataType().Should().Be("float");
            typeof(double?).ToSqlDataType().Should().Be("float");
            typeof(DateTime).ToSqlDataType().Should().Be("dateTime");
            typeof(DateTime?).ToSqlDataType().Should().Be("dateTime");
            typeof(Guid).ToSqlDataType().Should().Be("uniqueIdentifier");
            typeof(object).ToSqlDataType().Should().Be("varBinary");
            typeof(string).ToSqlDataType().Should().Be("nVarChar(255)");
        }
    }
}
