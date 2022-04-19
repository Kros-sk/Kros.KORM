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
using Xunit.Abstractions;

namespace Kros.KORM.UnitTests.Converter
{
    public class NullValuesInDatabaseShould : DatabaseTestBase
    {
        private readonly ITestOutputHelper _output;

        public NullValuesInDatabaseShould(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Helpers

        private const string ConverterValue = "NOT DB NULL";
        private const string ConverterValueDbNull = "DB NULL";

        private class TestDataClass
        {
            public TestDataClass() : this(initProperties: true) { }

            private TestDataClass(bool initProperties)
            {
                if (initProperties)
                {
                    ObjectVal = TestData.GetDataByType<TestDataSubClass>();
                    StrVal = TestData.GetDataByType<string>();

                    BoolVal = TestData.GetDataByType<bool>();
                    ByteVal = TestData.GetDataByType<byte>();
                    SByteVal = TestData.GetDataByType<sbyte>();
                    Int16Val = TestData.GetDataByType<short>();
                    UInt16Val = TestData.GetDataByType<ushort>();
                    Int32Val = TestData.GetDataByType<int>();
                    UInt32Val = TestData.GetDataByType<uint>();
                    Int64Val = TestData.GetDataByType<long>();
                    UInt64Val = TestData.GetDataByType<ulong>();
                    CharVal = TestData.GetDataByType<char>();
                    DoubleVal = TestData.GetDataByType<double>();
                    SingleVal = TestData.GetDataByType<float>();
                    DecimalVal = TestData.GetDataByType<decimal>();

                    NullableBoolVal = TestData.GetDataByType<bool>();
                    NullableByteVal = TestData.GetDataByType<byte>();
                    NullableSByteVal = TestData.GetDataByType<sbyte>();
                    NullableInt16Val = TestData.GetDataByType<short>();
                    NullableUInt16Val = TestData.GetDataByType<ushort>();
                    NullableInt32Val = TestData.GetDataByType<int>();
                    NullableUInt32Val = TestData.GetDataByType<uint>();
                    NullableInt64Val = TestData.GetDataByType<long>();
                    NullableUInt64Val = TestData.GetDataByType<ulong>();
                    NullableCharVal = TestData.GetDataByType<char>();
                    NullableDoubleVal = TestData.GetDataByType<double>();
                    NullableSingleVal = TestData.GetDataByType<float>();
                    NullableDecimalVal = TestData.GetDataByType<decimal>();

                    GuidVal = TestData.GetDataByType<Guid>();
                    NullableGuidVal = TestData.GetDataByType<Guid>();
                    DateTimeVal = TestData.GetDataByType<DateTime>();
                    NullableDateTimeVal = TestData.GetDataByType<DateTime>();
                    CustomStructVal = TestData.GetDataByType<TestDataStruct>();
                    NullableCustomStructVal = TestData.GetDataByType<TestDataStruct>();
                }
            }

            public static TestDataClass CreateWithNulledProperties() => new TestDataClass(initProperties: false);

            public TestDataSubClass ObjectVal { get; set; }
            public string StrVal { get; set; }

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

            public Guid GuidVal { get; set; }
            public Guid? NullableGuidVal { get; set; }
            public DateTime DateTimeVal { get; set; }
            public DateTime? NullableDateTimeVal { get; set; }
            public TestDataStruct CustomStructVal { get; set; }
            public TestDataStruct? NullableCustomStructVal { get; set; }
        }

        private class ConvertedTestDataClass
        {
            public ConvertedTestDataClass() : this(null) { }

            public ConvertedTestDataClass(string value)
            {
                ObjectVal = value;
                StrVal = value;

                BoolVal = value;
                ByteVal = value;
                SByteVal = value;
                Int16Val = value;
                UInt16Val = value;
                Int32Val = value;
                UInt32Val = value;
                Int64Val = value;
                UInt64Val = value;
                CharVal = value;
                DoubleVal = value;
                SingleVal = value;
                DecimalVal = value;

                NullableBoolVal = value;
                NullableByteVal = value;
                NullableSByteVal = value;
                NullableInt16Val = value;
                NullableUInt16Val = value;
                NullableInt32Val = value;
                NullableUInt32Val = value;
                NullableInt64Val = value;
                NullableUInt64Val = value;
                NullableCharVal = value;
                NullableDoubleVal = value;
                NullableSingleVal = value;
                NullableDecimalVal = value;

                GuidVal = value;
                NullableGuidVal = value;
                DateTimeVal = value;
                NullableDateTimeVal = value;
                CustomStructVal = value;
                NullableCustomStructVal = value;
            }

            public string ObjectVal { get; set; }
            public string StrVal { get; set; }

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

            public string GuidVal { get; set; }
            public string NullableGuidVal { get; set; }
            public string DateTimeVal { get; set; }
            public string NullableDateTimeVal { get; set; }
            public string CustomStructVal { get; set; }
            public string NullableCustomStructVal { get; set; }
        }

        private record TestDataRecord
        (
            string StrVal = "Lorem",
            bool BoolVal = true,
            byte ByteVal = 1,
            sbyte SByteVal = 2,
            short Int16Val = 3,
            ushort UInt16Val = 4,
            int Int32Val = 5,
            uint UInt32Val = 6,
            long Int64Val = 7,
            ulong UInt64Val = 8,
            char CharVal = 'a',
            double DoubleVal = 3.14,
            float SingleVal = 6.28f,
            bool? NullableBoolVal = true,
            byte? NullableByteVal = 1,
            sbyte? NullableSByteVal = 2,
            short? NullableInt16Val = 3,
            ushort? NullableUInt16Val = 4,
            int? NullableInt32Val = 5,
            uint? NullableUInt32Val = 6,
            long? NullableInt64Val = 7,
            ulong? NullableUInt64Val = 8,
            char? NullableCharVal = 'a',
            double? NullableDoubleVal = 3.14,
            float? NullableSingleVal = 6.28f
        );

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
            string NullableSingleVal = null
        );

        private class TestDataSubClass
        {
            public string Value { get; set; }
        }

        private struct TestDataStruct
        {
            public int IntVal;
            public string StringVal;
        }

        private class TestConverter : IConverter
        {
            public object Convert(object value) => value is null ? ConverterValueDbNull : ConverterValue;
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

                reader.GetValue(Arg.Any<int>()).Returns(ci => TestData.GetDataByType(_info[ci.Arg<int>()].DbType));
                reader.GetBoolean(Arg.Any<int>()).Returns(TestData.GetDataByType<bool>());
                reader.GetByte(Arg.Any<int>()).Returns(TestData.GetDataByType<byte>());
                reader.GetChar(Arg.Any<int>()).Returns(TestData.GetDataByType<char>());
                reader.GetDateTime(Arg.Any<int>()).Returns(TestData.GetDataByType<DateTime>());
                reader.GetDecimal(Arg.Any<int>()).Returns(TestData.GetDataByType<decimal>());
                reader.GetDouble(Arg.Any<int>()).Returns(TestData.GetDataByType<double>());
                reader.GetFloat(Arg.Any<int>()).Returns(TestData.GetDataByType<float>());
                reader.GetGuid(Arg.Any<int>()).Returns(TestData.GetDataByType<Guid>());
                reader.GetInt16(Arg.Any<int>()).Returns(TestData.GetDataByType<short>());
                reader.GetInt32(Arg.Any<int>()).Returns(TestData.GetDataByType<int>());
                reader.GetInt64(Arg.Any<int>()).Returns(TestData.GetDataByType<long>());
                reader.GetString(Arg.Any<int>()).Returns(TestData.GetDataByType<string>());

                return reader;
            }
        }

        private static class TestData
        {
            private static readonly Dictionary<Type, object> _dataByType = new()
            {
                { typeof(TestDataSubClass), new TestDataSubClass() { Value = "Lorem" } },
                { typeof(string), "Lorem" },
                { typeof(bool), true },
                { typeof(byte), (byte)1 },
                { typeof(sbyte), (sbyte)2 },
                { typeof(short), (short)3 },
                { typeof(ushort), (ushort)4 },
                { typeof(int), (int)5 },
                { typeof(uint), (uint)6 },
                { typeof(long), (long)7 },
                { typeof(ulong), (ulong)8 },
                { typeof(char), 'a' },
                { typeof(double), 3.14 },
                { typeof(float), (float)6.28 },
                { typeof(decimal), (decimal)9.42 },
                { typeof(Guid), new Guid("12345678-1234-1234-1234-123456789012") },
                { typeof(DateTime), new DateTime(1978, 12, 10) },
                { typeof(TestDataStruct), new TestDataStruct() { IntVal = 42, StringVal = "Lorem" } }
            };

            public static T GetDataByType<T>() => (T)GetDataByType(typeof(T));

            public static object GetDataByType(Type dataType)
            {
                Type realType = Nullable.GetUnderlyingType(dataType);
                if (realType is null)
                {
                    realType = dataType;
                }
                if (_dataByType.TryGetValue(realType, out object value))
                {
                    return value;
                }
                return null;
            }
        }

        #endregion

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PropagateToDataClass(bool simulateDbNull)
        {
            ILGeneratorHelper.Logger = _output.WriteLine;
            TestDataHelper<TestDataClass, TestDataClass> info = new(simulateDbNull: simulateDbNull, useConverter: false);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataClass> factory = modelFactory.GetFactory<TestDataClass>(info.DataReader);

            TestDataClass actual = factory(info.DataReader);
            TestDataClass expected = simulateDbNull ? TestDataClass.CreateWithNulledProperties() : new();
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BeConsumedByConvertersInDataClass(bool simulateDbNull)
        {
            ILGeneratorHelper.Logger = _output.WriteLine;
            TestDataHelper<ConvertedTestDataClass, TestDataClass> info = new(simulateDbNull: simulateDbNull, useConverter: true);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, ConvertedTestDataClass> factory = modelFactory.GetFactory<ConvertedTestDataClass>(info.DataReader);

            ConvertedTestDataClass actual = factory(info.DataReader);
            ConvertedTestDataClass expected = new(simulateDbNull ? ConverterValueDbNull : ConverterValue);
            actual.Should().BeEquivalentTo(expected);
        }

        //[Fact]
        //public void PropagateToDataRecord()
        //{
        //    ILGeneratorHelper.Logger = _output.WriteLine;
        //    TestDataHelper<TestDataRecord, TestDataRecord> info = new(useConverter: false);
        //    DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
        //    Func<IDataReader, TestDataRecord> factory = modelFactory.GetFactory<TestDataRecord>(info.DataReader);

        //    TestDataRecord actual = factory(info.DataReader);
        //    TestDataRecord expected = new()
        //    {
        //        StrVal = null,
        //        BoolVal = false,
        //        ByteVal = 0,
        //        SByteVal = 0,
        //        Int16Val = 0,
        //        UInt16Val = 0,
        //        Int32Val = 0,
        //        UInt32Val = 0,
        //        Int64Val = 0,
        //        UInt64Val = 0,
        //        CharVal = '\0',
        //        DoubleVal = 0,
        //        SingleVal = 0,
        //        NullableBoolVal = null,
        //        NullableByteVal = null,
        //        NullableSByteVal = null,
        //        NullableInt16Val = null,
        //        NullableUInt16Val = null,
        //        NullableInt32Val = null,
        //        NullableUInt32Val = null,
        //        NullableInt64Val = null,
        //        NullableUInt64Val = null,
        //        NullableCharVal = null,
        //        NullableDoubleVal = null,
        //        NullableSingleVal = null
        //    };
        //    actual.Should().BeEquivalentTo(expected);
        //}

        //[Fact]
        //public void BeConsumedByConvertersInDataRecord()
        //{
        //    ILGeneratorHelper.Logger = _output.WriteLine;
        //    ILGeneratorHelper.Logger = _output.WriteLine;
        //    TestDataHelper<ConvertedTestDataRecord, TestDataRecord> info = new(useConverter: true);
        //    DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
        //    Func<IDataReader, ConvertedTestDataRecord> factory = modelFactory.GetFactory<ConvertedTestDataRecord>(info.DataReader);

        //    ConvertedTestDataRecord actual = factory(info.DataReader);
        //    ConvertedTestDataRecord expected = new()
        //    {
        //        StrVal = ConvertedValue,
        //        BoolVal = ConvertedValue,
        //        ByteVal = ConvertedValue,
        //        SByteVal = ConvertedValue,
        //        Int16Val = ConvertedValue,
        //        UInt16Val = ConvertedValue,
        //        Int32Val = ConvertedValue,
        //        UInt32Val = ConvertedValue,
        //        Int64Val = ConvertedValue,
        //        UInt64Val = ConvertedValue,
        //        CharVal = ConvertedValue,
        //        DoubleVal = ConvertedValue,
        //        SingleVal = ConvertedValue,
        //        NullableBoolVal = ConvertedValue,
        //        NullableByteVal = ConvertedValue,
        //        NullableSByteVal = ConvertedValue,
        //        NullableInt16Val = ConvertedValue,
        //        NullableUInt16Val = ConvertedValue,
        //        NullableInt32Val = ConvertedValue,
        //        NullableUInt32Val = ConvertedValue,
        //        NullableInt64Val = ConvertedValue,
        //        NullableUInt64Val = ConvertedValue,
        //        NullableCharVal = ConvertedValue,
        //        NullableDoubleVal = ConvertedValue,
        //        NullableSingleVal = ConvertedValue
        //    };
        //    actual.Should().BeEquivalentTo(expected);
        //}
    }
}
