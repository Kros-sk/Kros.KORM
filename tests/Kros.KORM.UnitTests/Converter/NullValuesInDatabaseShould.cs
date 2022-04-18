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
        #region Helpers

        private const string ConvertedValue = "Lorem Ipsum";

        private class TestDataClass
        {
            public TestDataClass() : this(true) { }

            public TestDataClass(bool initProperties)
            {
                if (initProperties)
                {
                    ObjectVal = new TestDataSubClass() { Value = "Lorem" };
                    StrVal = "Lorem";

                    BoolVal = true;
                    ByteVal = 1;
                    SByteVal = 2;
                    Int16Val = 3;
                    UInt16Val = 4;
                    Int32Val = 5;
                    UInt32Val = 6;
                    Int64Val = 7;
                    UInt64Val = 8;
                    CharVal = 'a';
                    DoubleVal = 3.14;
                    SingleVal = 6.28F;

                    NullableBoolVal = true;
                    NullableByteVal = 1;
                    NullableSByteVal = 2;
                    NullableInt16Val = 3;
                    NullableUInt16Val = 4;
                    NullableInt32Val = 5;
                    NullableUInt32Val = 6;
                    NullableInt64Val = 7;
                    NullableUInt64Val = 8;
                    NullableCharVal = 'a';
                    NullableDoubleVal = 3.14;
                    NullableSingleVal = 6.28F;

                    DateTimeVal = new DateTime(1978, 12, 10);
                    NullableDateTimeVal = new DateTime(1978, 12, 10);
                    CustomStructVal = new TestDataStruct() { IntVal = 42, StringVal = "Lorem" };
                    NullableCustomStructVal = new TestDataStruct() { IntVal = 42, StringVal = "Lorem" };
                }
            }

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

            public DateTime DateTimeVal { get; set; }
            public DateTime? NullableDateTimeVal { get; set; }
            public TestDataStruct CustomStructVal { get; set; }
            public TestDataStruct? NullableCustomStructVal { get; set; }
        }

        private class ConvertedTestDataClass
        {
            public ConvertedTestDataClass() : this(false) { }

            public ConvertedTestDataClass(bool initProperties)
            {
                if (initProperties)
                {
                    ObjectVal = ConvertedValue;
                    StrVal = ConvertedValue;

                    BoolVal = ConvertedValue;
                    ByteVal = ConvertedValue;
                    SByteVal = ConvertedValue;
                    Int16Val = ConvertedValue;
                    UInt16Val = ConvertedValue;
                    Int32Val = ConvertedValue;
                    UInt32Val = ConvertedValue;
                    Int64Val = ConvertedValue;
                    UInt64Val = ConvertedValue;
                    CharVal = ConvertedValue;
                    DoubleVal = ConvertedValue;
                    SingleVal = ConvertedValue;

                    NullableBoolVal = ConvertedValue;
                    NullableByteVal = ConvertedValue;
                    NullableSByteVal = ConvertedValue;
                    NullableInt16Val = ConvertedValue;
                    NullableUInt16Val = ConvertedValue;
                    NullableInt32Val = ConvertedValue;
                    NullableUInt32Val = ConvertedValue;
                    NullableInt64Val = ConvertedValue;
                    NullableUInt64Val = ConvertedValue;
                    NullableCharVal = ConvertedValue;
                    NullableDoubleVal = ConvertedValue;
                    NullableSingleVal = ConvertedValue;

                    DateTimeVal = ConvertedValue;
                    NullableDateTimeVal = ConvertedValue;
                    CustomStructVal = ConvertedValue;
                    NullableCustomStructVal = ConvertedValue;
                }
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

            public string DateTimeVal { get; set; }
            public string NullableDateTimeVal { get; set; }
            public string CustomStructVal { get; set; }
            public string NullableCustomStructVal { get; set; }
        }

        private record TestDataRecord
        (
            //string StrVal = "Lorem",
            bool BoolVal = true,
            byte ByteVal = 1
            //sbyte SByteVal = 2,
            //short Int16Val = 3,
            //ushort UInt16Val = 4,
            //int Int32Val = 5,
            //uint UInt32Val = 6,
            //long Int64Val = 7,
            //ulong UInt64Val = 8,
            //char CharVal = 'a',
            //double DoubleVal = 3.14,
            //float SingleVal = 6.28f
            //bool? NullableBoolVal = true,
            //byte? NullableByteVal = 1,
            //sbyte? NullableSByteVal = 2,
            //short? NullableInt16Val = 3,
            //ushort? NullableUInt16Val = 4,
            //int? NullableInt32Val = 5,
            //uint? NullableUInt32Val = 6,
            //long? NullableInt64Val = 7,
            //ulong? NullableUInt64Val = 8,
            //char? NullableCharVal = 'a',
            //double? NullableDoubleVal = 3.14,
            //float? NullableSingleVal = 6.28f
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
            public object Convert(object value) => ConvertedValue;
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class PropInfo
        {
            public string Name { get; set; }
            public Type DbType { get; set; }
        }

        private class TestDataInfo<TClientData, TDbData>
        {
            private readonly Dictionary<int, PropInfo> _info = new();
            private readonly IDatabaseMapper _databaseMapper;
            private readonly IDataReader _dataReader;

            public TestDataInfo(bool useConverters)
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
                    i++;
                }
                _databaseMapper = CreateDatabaseMapper<TClientData>(useConverters);
                _dataReader = CreateDataReader();
            }

            public int Count => _info.Count;
            public IDatabaseMapper DatabaseMapper => _databaseMapper;
            public IDataReader DataReader => _dataReader;

            private static IDatabaseMapper CreateDatabaseMapper<T>(bool useConverters)
            {
                IDatabaseMapper mapper = Substitute.For<IDatabaseMapper>();

                ConventionModelMapper modelMapper = new();
                TableInfo tableInfo = modelMapper.GetTableInfo<T>();
                if (useConverters)
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

            private IDataReader CreateDataReader()
            {
                IDataReader reader = Substitute.For<IDataReader>();

                reader.FieldCount.Returns(Count);
                reader.GetFieldType(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].DbType);
                reader.GetName(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].Name);
                reader.IsDBNull(Arg.Any<int>()).Returns(ci => true);

                return reader;
            }
        }

        #endregion

        [Fact]
        public void PropagateToDataClass()
        {
            TestDataInfo<TestDataClass, TestDataClass> info = new(false);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataClass> factory = modelFactory.GetFactory<TestDataClass>(info.DataReader);

            TestDataClass actual = factory(info.DataReader);
            TestDataClass expected = new(initProperties: false);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void BeConsumedByConvertersInDataClass()
        {
            TestDataInfo<ConvertedTestDataClass, TestDataClass> info = new(true);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, ConvertedTestDataClass> factory = modelFactory.GetFactory<ConvertedTestDataClass>(info.DataReader);

            ConvertedTestDataClass actual = factory(info.DataReader);
            ConvertedTestDataClass expected = new(initProperties: true);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void PropagateToDataRecord()
        {
            TestDataInfo<TestDataRecord, TestDataRecord> info = new(false);
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataRecord> factory = modelFactory.GetFactory<TestDataRecord>(info.DataReader);

            TestDataRecord actual = factory(info.DataReader);
            TestDataRecord expected = new()
            {
                //StrVal = null,
                BoolVal = false,
                ByteVal = 0,
                //SByteVal = 0,
                //Int16Val = 0,
                //UInt16Val = 0,
                //Int32Val = 0,
                //UInt32Val = 0,
                //Int64Val = 0,
                //UInt64Val = 0,
                //CharVal = '\0',
                //DoubleVal = 0,
                //SingleVal = 0,
                //NullableBoolVal = null,
                //NullableByteVal = null,
                //NullableSByteVal = null,
                //NullableInt16Val = null,
                //NullableUInt16Val = null,
                //NullableInt32Val = null,
                //NullableUInt32Val = null,
                //NullableInt64Val = null,
                //NullableUInt64Val = null,
                //NullableCharVal = null,
                //NullableDoubleVal = null,
                //NullableSingleVal = null
            };
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void BeConsumedByConvertersInDataRecord()
        {
            throw new NotImplementedException();
        }
    }
}
