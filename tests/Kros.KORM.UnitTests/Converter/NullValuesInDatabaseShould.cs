using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.UnitTests.Base;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class NullValuesInDatabaseShould : DatabaseTestBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PropagateToDataClass(bool simulateDbNull)
        {
            TestDataHelper<TestDataClass, TestDataClass> info = new(simulateDbNull: simulateDbNull, useConverter: false);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataClass> factory = modelFactory.GetFactory<TestDataClass>(info.DataReader);

            TestDataClass actual = factory(info.DataReader);
            TestDataClass expected = simulateDbNull
                ? TestDataClass.CreateWithNulledProperties()
                : TestDataClass.CreateWithDbValuesProperties();
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BeConsumedByConvertersInDataClass(bool simulateDbNull)
        {
            TestDataHelper<ConvertedTestDataClass, TestDataClass> info = new(simulateDbNull: simulateDbNull, useConverter: true);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, ConvertedTestDataClass> factory = modelFactory.GetFactory<ConvertedTestDataClass>(info.DataReader);

            ConvertedTestDataClass actual = factory(info.DataReader);
            ConvertedTestDataClass expected = simulateDbNull ? new(ConverterDbNullValue) : new(ConverterDefaultValue);
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PropagateToDataRecord(bool simulateDbNull)
        {
            TestDataHelper<TestDataRecord, TestDataRecord> info = new(simulateDbNull: simulateDbNull, useConverter: false);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataRecord> factory = modelFactory.GetFactory<TestDataRecord>(info.DataReader);

            TestDataRecord actual = factory(info.DataReader);
            TestDataRecord expected = simulateDbNull
                ? TestDataRecord.CreateWithNulledProperties()
                : TestDataRecord.CreateWithDbValuesProperties();
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BeConsumedByConvertersInDataRecord(bool simulateDbNull)
        {
            TestDataHelper<ConvertedTestDataRecord, TestDataRecord> info = new(simulateDbNull: simulateDbNull, useConverter: true);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, ConvertedTestDataRecord> factory = modelFactory.GetFactory<ConvertedTestDataRecord>(info.DataReader);

            ConvertedTestDataRecord actual = factory(info.DataReader);
            ConvertedTestDataRecord expected = simulateDbNull
                ? ConvertedTestDataRecord.Create(ConverterDbNullValue)
                : ConvertedTestDataRecord.Create(ConverterDefaultValue);
            actual.Should().BeEquivalentTo(expected);
        }

        #region Helpers

        private const string ConverterDefaultValue = "Default";
        private const string ConverterDbNullValue = "DB NULL";

        private class TestDataClass
        {
            public TestDataClass() : this(initProperties: true) { }

            private TestDataClass(bool initProperties)
            {
                if (initProperties)
                {
                    BoolVal = InitialDataSource.BoolVal;
                    ByteVal = InitialDataSource.ByteVal;
                    SByteVal = InitialDataSource.SByteVal;
                    Int16Val = InitialDataSource.Int16Val;
                    UInt16Val = InitialDataSource.UInt16Val;
                    Int32Val = InitialDataSource.Int32Val;
                    UInt32Val = InitialDataSource.UInt32Val;
                    Int64Val = InitialDataSource.Int64Val;
                    UInt64Val = InitialDataSource.UInt64Val;
                    CharVal = InitialDataSource.CharVal;
                    DoubleVal = InitialDataSource.DoubleVal;
                    SingleVal = InitialDataSource.SingleVal;
                    DecimalVal = InitialDataSource.DecimalVal;

                    NullableBoolVal = InitialDataSource.BoolVal;
                    NullableByteVal = InitialDataSource.ByteVal;
                    NullableSByteVal = InitialDataSource.SByteVal;
                    NullableInt16Val = InitialDataSource.Int16Val;
                    NullableUInt16Val = InitialDataSource.UInt16Val;
                    NullableInt32Val = InitialDataSource.Int32Val;
                    NullableUInt32Val = InitialDataSource.UInt32Val;
                    NullableInt64Val = InitialDataSource.Int64Val;
                    NullableUInt64Val = InitialDataSource.UInt64Val;
                    NullableCharVal = InitialDataSource.CharVal;
                    NullableDoubleVal = InitialDataSource.DoubleVal;
                    NullableSingleVal = InitialDataSource.SingleVal;
                    NullableDecimalVal = InitialDataSource.DecimalVal;

                    ObjectVal = InitialDataSource.ObjectVal;
                    StringVal = InitialDataSource.StringVal;
                    GuidVal = InitialDataSource.GuidVal;
                    NullableGuidVal = InitialDataSource.GuidVal;
                    DateTimeVal = InitialDataSource.DateTimeVal;
                    NullableDateTimeVal = InitialDataSource.DateTimeVal;
                    CustomStructVal = InitialDataSource.CustomStructVal;
                    NullableCustomStructVal = InitialDataSource.CustomStructVal;
                    CustomEnumVal = InitialDataSource.CustomEnumVal;
                    NullableCustomEnumVal = InitialDataSource.CustomEnumVal;
                    CustomEnumWithoutZeroVal = InitialDataSource.CustomEnumWithoutZeroVal;
                    NullableCustomEnumWithoutZeroVal = InitialDataSource.CustomEnumWithoutZeroVal;
                }
            }

            public static TestDataClass CreateWithNulledProperties() => new(initProperties: false);
            public static TestDataClass CreateWithDbValuesProperties() => new(initProperties: false)
            {
                BoolVal = DbDataSource.BoolVal,
                ByteVal = DbDataSource.ByteVal,
                SByteVal = DbDataSource.SByteVal,
                Int16Val = DbDataSource.Int16Val,
                UInt16Val = DbDataSource.UInt16Val,
                Int32Val = DbDataSource.Int32Val,
                UInt32Val = DbDataSource.UInt32Val,
                Int64Val = DbDataSource.Int64Val,
                UInt64Val = DbDataSource.UInt64Val,
                CharVal = DbDataSource.CharVal,
                DoubleVal = DbDataSource.DoubleVal,
                SingleVal = DbDataSource.SingleVal,
                DecimalVal = DbDataSource.DecimalVal,

                NullableBoolVal = DbDataSource.BoolVal,
                NullableByteVal = DbDataSource.ByteVal,
                NullableSByteVal = DbDataSource.SByteVal,
                NullableInt16Val = DbDataSource.Int16Val,
                NullableUInt16Val = DbDataSource.UInt16Val,
                NullableInt32Val = DbDataSource.Int32Val,
                NullableUInt32Val = DbDataSource.UInt32Val,
                NullableInt64Val = DbDataSource.Int64Val,
                NullableUInt64Val = DbDataSource.UInt64Val,
                NullableCharVal = DbDataSource.CharVal,
                NullableDoubleVal = DbDataSource.DoubleVal,
                NullableSingleVal = DbDataSource.SingleVal,
                NullableDecimalVal = DbDataSource.DecimalVal,

                ObjectVal = DbDataSource.ObjectVal,
                StringVal = DbDataSource.StringVal,
                GuidVal = DbDataSource.GuidVal,
                NullableGuidVal = DbDataSource.GuidVal,
                DateTimeVal = DbDataSource.DateTimeVal,
                NullableDateTimeVal = DbDataSource.DateTimeVal,
                CustomStructVal = DbDataSource.CustomStructVal,
                NullableCustomStructVal = DbDataSource.CustomStructVal,
                CustomEnumVal = DbDataSource.CustomEnumVal,
                NullableCustomEnumVal = DbDataSource.CustomEnumVal,
                CustomEnumWithoutZeroVal = DbDataSource.CustomEnumWithoutZeroVal,
                NullableCustomEnumWithoutZeroVal = DbDataSource.CustomEnumWithoutZeroVal
            };

            public bool BoolVal { get; set; }
            public byte ByteVal { get; set; }
            public sbyte SByteVal { get; set; }
            public short Int16Val { get; set; }
            public ushort UInt16Val { get; set; }
            public int Int32Val { get; set; }
            public uint UInt32Val { get; set; }
            public long Int64Val { get; set; }
            public ulong UInt64Val { get; set; }
            public char CharVal { get; set; }
            public double DoubleVal { get; set; }
            public float SingleVal { get; set; }
            public decimal DecimalVal { get; set; }

            public bool? NullableBoolVal { get; set; }
            public byte? NullableByteVal { get; set; }
            public sbyte? NullableSByteVal { get; set; }
            public short? NullableInt16Val { get; set; }
            public ushort? NullableUInt16Val { get; set; }
            public int? NullableInt32Val { get; set; }
            public uint? NullableUInt32Val { get; set; }
            public long? NullableInt64Val { get; set; }
            public ulong? NullableUInt64Val { get; set; }
            public char? NullableCharVal { get; set; }
            public double? NullableDoubleVal { get; set; }
            public float? NullableSingleVal { get; set; }
            public decimal? NullableDecimalVal { get; set; }

            public string StringVal { get; set; }
            public TestDataSubClass ObjectVal { get; set; }
            public Guid GuidVal { get; set; }
            public Guid? NullableGuidVal { get; set; }
            public DateTime DateTimeVal { get; set; }
            public DateTime? NullableDateTimeVal { get; set; }
            public TestDataStruct CustomStructVal { get; set; }
            public TestDataStruct? NullableCustomStructVal { get; set; }
            public TestDataEnum CustomEnumVal { get; set; }
            public TestDataEnum? NullableCustomEnumVal { get; set; }
            public TestDataEnumWithoutZero CustomEnumWithoutZeroVal { get; set; }
            public TestDataEnumWithoutZero? NullableCustomEnumWithoutZeroVal { get; set; }
        }

        private class ConvertedTestDataClass
        {
            public ConvertedTestDataClass() : this(null) { }

            public ConvertedTestDataClass(string value)
            {
                foreach (PropertyInfo prop in typeof(ConvertedTestDataClass).GetProperties())
                {
                    prop.SetValue(this, value);
                }
            }

            public string BoolVal { get; set; }
            public string ByteVal { get; set; }
            public string SByteVal { get; set; }
            public string Int16Val { get; set; }
            public string UInt16Val { get; set; }
            public string Int32Val { get; set; }
            public string UInt32Val { get; set; }
            public string Int64Val { get; set; }
            public string UInt64Val { get; set; }
            public string CharVal { get; set; }
            public string DoubleVal { get; set; }
            public string SingleVal { get; set; }
            public string DecimalVal { get; set; }

            public string NullableBoolVal { get; set; }
            public string NullableByteVal { get; set; }
            public string NullableSByteVal { get; set; }
            public string NullableInt16Val { get; set; }
            public string NullableUInt16Val { get; set; }
            public string NullableInt32Val { get; set; }
            public string NullableUInt32Val { get; set; }
            public string NullableInt64Val { get; set; }
            public string NullableUInt64Val { get; set; }
            public string NullableCharVal { get; set; }
            public string NullableDoubleVal { get; set; }
            public string NullableSingleVal { get; set; }
            public string NullableDecimalVal { get; set; }

            public string StringVal { get; set; }
            public string ObjectVal { get; set; }
            public string GuidVal { get; set; }
            public string NullableGuidVal { get; set; }
            public string DateTimeVal { get; set; }
            public string NullableDateTimeVal { get; set; }
            public string CustomStructVal { get; set; }
            public string NullableCustomStructVal { get; set; }
            public string CustomEnumVal { get; set; }
            public string NullableCustomEnumVal { get; set; }
            public string CustomEnumWithoutZeroVal { get; set; }
            public string NullableCustomEnumWithoutZeroVal { get; set; }
        }

        private record TestDataRecord
        (
            bool BoolVal = InitialDataSource.BoolVal,
            byte ByteVal = InitialDataSource.ByteVal,
            sbyte SByteVal = InitialDataSource.SByteVal,
            short Int16Val = InitialDataSource.Int16Val,
            ushort UInt16Val = InitialDataSource.UInt16Val,
            int Int32Val = InitialDataSource.Int32Val,
            uint UInt32Val = InitialDataSource.UInt32Val,
            long Int64Val = InitialDataSource.Int64Val,
            ulong UInt64Val = InitialDataSource.UInt64Val,
            char CharVal = InitialDataSource.CharVal,
            double DoubleVal = InitialDataSource.DoubleVal,
            float SingleVal = InitialDataSource.SingleVal,
            decimal DecimalVal = InitialDataSource.DecimalVal,
            TestDataEnum CustomEnumVal = InitialDataSource.CustomEnumVal,
            TestDataEnumWithoutZero CustomEnumWithoutZeroVal = InitialDataSource.CustomEnumWithoutZeroVal,

            bool? NullableBoolVal = InitialDataSource.BoolVal,
            byte? NullableByteVal = InitialDataSource.ByteVal,
            sbyte? NullableSByteVal = InitialDataSource.SByteVal,
            short? NullableInt16Val = InitialDataSource.Int16Val,
            ushort? NullableUInt16Val = InitialDataSource.UInt16Val,
            int? NullableInt32Val = InitialDataSource.Int32Val,
            uint? NullableUInt32Val = InitialDataSource.UInt32Val,
            long? NullableInt64Val = InitialDataSource.Int64Val,
            ulong? NullableUInt64Val = InitialDataSource.UInt64Val,
            char? NullableCharVal = InitialDataSource.CharVal,
            double? NullableDoubleVal = InitialDataSource.DoubleVal,
            float? NullableSingleVal = InitialDataSource.SingleVal,
            decimal? NullableDecimalVal = InitialDataSource.DecimalVal,
            TestDataEnum? NullableCustomEnumVal = InitialDataSource.CustomEnumVal,
            TestDataEnumWithoutZero? NullableCustomEnumWithoutZeroVal = InitialDataSource.CustomEnumWithoutZeroVal,

            string StrVal = InitialDataSource.StringVal
        )
        {
            public static TestDataRecord CreateWithNulledProperties() => new()
            {
                BoolVal = false,
                ByteVal = 0,
                SByteVal = 0,
                Int16Val = 0,
                UInt16Val = 0,
                Int32Val = 0,
                UInt32Val = 0,
                Int64Val = 0,
                UInt64Val = 0,
                CharVal = '\0',
                DoubleVal = 0,
                SingleVal = 0,
                DecimalVal = 0,
                CustomEnumVal = TestDataEnum.Zero,
                CustomEnumWithoutZeroVal = (TestDataEnumWithoutZero)0,
                NullableBoolVal = null,
                NullableByteVal = null,
                NullableSByteVal = null,
                NullableInt16Val = null,
                NullableUInt16Val = null,
                NullableInt32Val = null,
                NullableUInt32Val = null,
                NullableInt64Val = null,
                NullableUInt64Val = null,
                NullableCharVal = null,
                NullableDoubleVal = null,
                NullableSingleVal = null,
                NullableDecimalVal = null,
                NullableCustomEnumVal = null,
                NullableCustomEnumWithoutZeroVal = null,
                StrVal = null
            };

            public static TestDataRecord CreateWithDbValuesProperties() => new()
            {
                BoolVal = DbDataSource.BoolVal,
                ByteVal = DbDataSource.ByteVal,
                SByteVal = DbDataSource.SByteVal,
                Int16Val = DbDataSource.Int16Val,
                UInt16Val = DbDataSource.UInt16Val,
                Int32Val = DbDataSource.Int32Val,
                UInt32Val = DbDataSource.UInt32Val,
                Int64Val = DbDataSource.Int64Val,
                UInt64Val = DbDataSource.UInt64Val,
                CharVal = DbDataSource.CharVal,
                DoubleVal = DbDataSource.DoubleVal,
                SingleVal = DbDataSource.SingleVal,
                DecimalVal = DbDataSource.DecimalVal,
                CustomEnumVal = DbDataSource.CustomEnumVal,
                CustomEnumWithoutZeroVal = DbDataSource.CustomEnumWithoutZeroVal,

                NullableBoolVal = DbDataSource.BoolVal,
                NullableByteVal = DbDataSource.ByteVal,
                NullableSByteVal = DbDataSource.SByteVal,
                NullableInt16Val = DbDataSource.Int16Val,
                NullableUInt16Val = DbDataSource.UInt16Val,
                NullableInt32Val = DbDataSource.Int32Val,
                NullableUInt32Val = DbDataSource.UInt32Val,
                NullableInt64Val = DbDataSource.Int64Val,
                NullableUInt64Val = DbDataSource.UInt64Val,
                NullableCharVal = DbDataSource.CharVal,
                NullableDoubleVal = DbDataSource.DoubleVal,
                NullableSingleVal = DbDataSource.SingleVal,
                NullableDecimalVal = DbDataSource.DecimalVal,
                NullableCustomEnumVal = DbDataSource.CustomEnumVal,
                NullableCustomEnumWithoutZeroVal = DbDataSource.CustomEnumWithoutZeroVal,

                StrVal = DbDataSource.StringVal
            };
        }

        private record ConvertedTestDataRecord
        (
            string StrVal = null,
            string BoolVal = null,
            string ByteVal = null,
            string SByteVal = null,
            string Int16Val = null,
            string UInt16Val = null,
            string Int32Val = null,
            string UInt32Val = null,
            string Int64Val = null,
            string UInt64Val = null,
            string CharVal = null,
            string DoubleVal = null,
            string SingleVal = null,
            string DecimalVal = null,
            string CustomEnumVal = null,
            string CustomEnumWithoutZeroVal = null,

            string NullableBoolVal = null,
            string NullableByteVal = null,
            string NullableSByteVal = null,
            string NullableInt16Val = null,
            string NullableUInt16Val = null,
            string NullableInt32Val = null,
            string NullableUInt32Val = null,
            string NullableInt64Val = null,
            string NullableUInt64Val = null,
            string NullableCharVal = null,
            string NullableDoubleVal = null,
            string NullableSingleVal = null,
            string NullableDecimalVal = null,
            string NullableCustomEnumVal = null,
            string NullableCustomEnumWithoutZeroVal = null
        )
        {
            public static ConvertedTestDataRecord Create(string value)
            {
                ConvertedTestDataRecord data = new();
                foreach (PropertyInfo prop in typeof(ConvertedTestDataRecord).GetProperties())
                {
                    prop.SetValue(data, value);
                }
                return data;
            }
        }

        private class TestDataSubClass
        {
            public string Value { get; set; }
        }

        private struct TestDataStruct
        {
            public int IntVal;
            public string StringVal;
        }

        private enum TestDataEnum
        {
            Zero,
            One,
            Two
        }

        private enum TestDataEnumWithoutZero : long
        {
            Ten = 10_000_000_000,
            Twenty = 20_000_000_000
        }

        private class TestConverter : IConverter
        {
            public object Convert(object value) => value is null ? ConverterDbNullValue : ConverterDefaultValue;
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class PropInfo
        {
            public string Name { get; set; }
            public Type DbType { get; set; }
        }

        private class TestDataHelper<TClientData, TDbData>
        {
            private readonly Dictionary<int, PropInfo> _info = new();
            private readonly Dictionary<string, int> _nameToOrdinalMap = new();

            public TestDataHelper(bool simulateDbNull, bool useConverter)
            {
                Type dataType = typeof(TDbData);
                PropertyInfo[] props = dataType.GetProperties();
                int i = 0;
                foreach (PropertyInfo prop in props)
                {
                    _info.Add(i, new PropInfo()
                    {
                        Name = prop.Name,
                        DbType = prop.PropertyType
                    });
                    _nameToOrdinalMap.Add(prop.Name, i);
                    i++;
                }
                DatabaseMapper = CreateDatabaseMapper<TClientData>(useConverter);
                DataReader = CreateDataReader(simulateDbNull);
            }

            public int Count => _info.Count;
            public IDatabaseMapper DatabaseMapper { get; }
            public IDataReader DataReader { get; }

            private static IDatabaseMapper CreateDatabaseMapper<T>(bool useConverter)
            {
                IDatabaseMapper mapper = Substitute.For<IDatabaseMapper>();

                ConventionModelMapper modelMapper = new();
                TableInfo tableInfo = modelMapper.GetTableInfo<T>();
                if (useConverter)
                {
                    foreach (ColumnInfo column in tableInfo.Columns)
                    {
                        column.Converter = new TestConverter();
                    }
                }
                mapper.GetTableInfo<T>().Returns(tableInfo);
                mapper.GetTableInfo(typeof(T)).Returns(tableInfo);

                return mapper;
            }

            private IDataReader CreateDataReader(bool simulateDbNull)
            {
                IDataReader reader = Substitute.For<IDataReader>();

                reader.FieldCount.Returns(Count);
                reader.GetOrdinal(Arg.Any<string>()).Returns(ci => _nameToOrdinalMap[ci.Arg<string>()]);
                reader.GetName(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].Name);
                reader.GetFieldType(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].DbType);
                reader.IsDBNull(Arg.Any<int>()).Returns(ci => simulateDbNull);

                reader.GetValue(Arg.Any<int>()).Returns(ci => DbDataSource.GetDataByType(_info[ci.Arg<int>()].DbType));
                reader.GetBoolean(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<bool>());
                reader.GetByte(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<byte>());
                reader.GetChar(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<char>());
                reader.GetDateTime(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<DateTime>());
                reader.GetDecimal(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<decimal>());
                reader.GetDouble(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<double>());
                reader.GetFloat(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<float>());
                reader.GetGuid(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<Guid>());
                reader.GetInt16(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<short>());
                reader.GetInt32(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<int>());
                reader.GetInt64(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<long>());
                reader.GetString(Arg.Any<int>()).Returns(DbDataSource.GetDataByType<string>());

                return reader;
            }
        }

        // Data for different types used as initial values for class. This values are set in constructor.
        private static class InitialDataSource
        {
            public const bool BoolVal = true;
            public const byte ByteVal = 1;
            public const sbyte SByteVal = 2;
            public const short Int16Val = 3;
            public const ushort UInt16Val = 4;
            public const int Int32Val = 5;
            public const uint UInt32Val = 6;
            public const long Int64Val = 7;
            public const ulong UInt64Val = 8;
            public const char CharVal = 'a';
            public const double DoubleVal = 1.14;
            public const float SingleVal = 2.14F;
            public const decimal DecimalVal = 3.14M;
            public const string StringVal = "Lorem";
            public static TestDataSubClass ObjectVal { get; } = new() { Value = "Lorem" };
            public static Guid GuidVal { get; } = new("12345678-1234-1234-1234-123456789012");
            public static DateTime DateTimeVal { get; } = new(1978, 12, 10);
            public static TestDataStruct CustomStructVal { get; } = new() { IntVal = 42, StringVal = "Lorem" };
            public const TestDataEnum CustomEnumVal = TestDataEnum.One;
            public const TestDataEnumWithoutZero CustomEnumWithoutZeroVal = TestDataEnumWithoutZero.Ten;
        }

        // Data for different types used to simulate values from database.
        private static class DbDataSource
        {
            public const bool BoolVal = false;
            public const byte ByteVal = 10;
            public const sbyte SByteVal = 20;
            public const short Int16Val = 30;
            public const ushort UInt16Val = 40;
            public const int Int32Val = 50;
            public const uint UInt32Val = 60;
            public const long Int64Val = 70;
            public const ulong UInt64Val = 80;
            public const char CharVal = 'z';
            public const double DoubleVal = 10.14;
            public const float SingleVal = 20.14F;
            public const decimal DecimalVal = 30.14M;
            public const string StringVal = "Ipsum";
            public static TestDataSubClass ObjectVal { get; } = new() { Value = "Ipsum" };
            public static Guid GuidVal { get; } = new("87654321-4321-4321-4321-210987654321");
            public static DateTime DateTimeVal { get; } = new(1985, 4, 16);
            public static TestDataStruct CustomStructVal { get; } = new() { IntVal = 24, StringVal = "Ipsum" };
            public const TestDataEnum CustomEnumVal = TestDataEnum.Two;
            public const TestDataEnumWithoutZero CustomEnumWithoutZeroVal = TestDataEnumWithoutZero.Twenty;

            private static readonly Dictionary<Type, object> _dataByType = new()
            {
                { typeof(string), StringVal },
                { typeof(bool), BoolVal },
                { typeof(byte), ByteVal },
                { typeof(sbyte), SByteVal },
                { typeof(short), Int16Val },
                { typeof(ushort), UInt16Val },
                { typeof(int), Int32Val },
                { typeof(uint), UInt32Val },
                { typeof(long), Int64Val },
                { typeof(ulong), UInt64Val },
                { typeof(char), CharVal },
                { typeof(double), DoubleVal },
                { typeof(float), SingleVal },
                { typeof(decimal), DecimalVal },
                { typeof(TestDataSubClass), ObjectVal },
                { typeof(Guid), GuidVal },
                { typeof(DateTime), DateTimeVal },
                { typeof(TestDataStruct), CustomStructVal },
                { typeof(TestDataEnum), CustomEnumVal },
                { typeof(TestDataEnumWithoutZero), CustomEnumWithoutZeroVal }
            };

            public static T GetDataByType<T>() => (T)GetDataByType(typeof(T));

            public static object GetDataByType(Type dataType)
            {
                Type realType = Nullable.GetUnderlyingType(dataType);
                if (realType is null)
                {
                    realType = dataType;
                }
                return _dataByType[realType];
            }
        }

        #endregion
    }
}
