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
            Assert.Equal(DbType.Boolean, typeof(bool).ToDbType());
            Assert.Equal(DbType.Boolean, typeof(bool?).ToDbType());
            Assert.Equal(DbType.Binary, typeof(byte[]).ToDbType());
            Assert.Equal(DbType.Byte, typeof(byte).ToDbType());
            Assert.Equal(DbType.Byte, typeof(byte?).ToDbType());
            Assert.Equal(DbType.SByte, typeof(sbyte).ToDbType());
            Assert.Equal(DbType.SByte, typeof(sbyte?).ToDbType());
            Assert.Equal(DbType.Int16, typeof(short).ToDbType());
            Assert.Equal(DbType.Int16, typeof(short?).ToDbType());
            Assert.Equal(DbType.UInt16, typeof(ushort).ToDbType());
            Assert.Equal(DbType.UInt16, typeof(ushort?).ToDbType());
            Assert.Equal(DbType.Int32, typeof(int).ToDbType());
            Assert.Equal(DbType.Int32, typeof(int?).ToDbType());
            Assert.Equal(DbType.UInt32, typeof(uint).ToDbType());
            Assert.Equal(DbType.UInt32, typeof(uint?).ToDbType());
            Assert.Equal(DbType.Int64, typeof(long).ToDbType());
            Assert.Equal(DbType.Int64, typeof(long?).ToDbType());
            Assert.Equal(DbType.UInt64, typeof(ulong).ToDbType());
            Assert.Equal(DbType.UInt64, typeof(ulong?).ToDbType());
            Assert.Equal(DbType.Single, typeof(float).ToDbType());
            Assert.Equal(DbType.Single, typeof(float?).ToDbType());
            Assert.Equal(DbType.Decimal, typeof(decimal).ToDbType());
            Assert.Equal(DbType.Decimal, typeof(decimal?).ToDbType());
            Assert.Equal(DbType.Double, typeof(double).ToDbType());
            Assert.Equal(DbType.Double, typeof(double?).ToDbType());
            Assert.Equal(DbType.DateTime, typeof(DateTime).ToDbType());
            Assert.Equal(DbType.DateTime, typeof(DateTime?).ToDbType());
            Assert.Equal(DbType.Guid, typeof(Guid).ToDbType());
            Assert.Equal(DbType.Binary, typeof(object).ToDbType());
            Assert.Equal(DbType.String, typeof(string).ToDbType());
        }

        [Fact]
        public void ConvertToSqlDataType()
        {
            Assert.Equal("bit", typeof(bool).ToSqlDataType());
            Assert.Equal("bit", typeof(bool?).ToSqlDataType());
            Assert.Equal("varBinary", typeof(byte[]).ToSqlDataType());
            Assert.Equal("tinyInt", typeof(byte).ToSqlDataType());
            Assert.Equal("tinyInt", typeof(byte?).ToSqlDataType());
            Assert.Equal("tinyInt", typeof(sbyte).ToSqlDataType());
            Assert.Equal("tinyInt", typeof(sbyte?).ToSqlDataType());
            Assert.Equal("smallInt", typeof(short).ToSqlDataType());
            Assert.Equal("smallInt", typeof(short?).ToSqlDataType());
            Assert.Equal("smallInt", typeof(ushort).ToSqlDataType());
            Assert.Equal("smallInt", typeof(ushort?).ToSqlDataType());
            Assert.Equal("int", typeof(int).ToSqlDataType());
            Assert.Equal("int", typeof(int?).ToSqlDataType());
            Assert.Equal("int", typeof(uint).ToSqlDataType());
            Assert.Equal("int", typeof(uint?).ToSqlDataType());
            Assert.Equal("bigInt", typeof(long).ToSqlDataType());
            Assert.Equal("bigInt", typeof(long?).ToSqlDataType());
            Assert.Equal("bigInt", typeof(ulong).ToSqlDataType());
            Assert.Equal("bigInt", typeof(ulong?).ToSqlDataType());
            Assert.Equal("real", typeof(float).ToSqlDataType());
            Assert.Equal("real", typeof(float?).ToSqlDataType());
            Assert.Equal("decimal", typeof(decimal).ToSqlDataType());
            Assert.Equal("decimal", typeof(decimal?).ToSqlDataType());
            Assert.Equal("float", typeof(double).ToSqlDataType());
            Assert.Equal("float", typeof(double?).ToSqlDataType());
            Assert.Equal("dateTime", typeof(DateTime).ToSqlDataType());
            Assert.Equal("dateTime", typeof(DateTime?).ToSqlDataType());
            Assert.Equal("uniqueIdentifier", typeof(Guid).ToSqlDataType());
            Assert.Equal("varBinary", typeof(object).ToSqlDataType());
            Assert.Equal("nVarChar(255)", typeof(string).ToSqlDataType());
        }
    }
}
