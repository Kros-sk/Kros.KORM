using FluentAssertions;
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

        private class TestDataItem
        {
            public TestDataItem() : this(true) { }

            public TestDataItem(bool initProperties)
            {
                if (initProperties)
                {
                    ObjectVal = new TestDataSubItem();
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

            public TestDataSubItem ObjectVal { get; set; }
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

        private class TestDataSubItem
        {
        }

        private struct TestDataStruct
        {
            public int IntVal;
            public string StringVal;
        }

        private class PropInfo
        {
            public string Name { get; set; }
            public Type PropertyType { get; set; }
        }

        private class TestDataInfo<T>
        {
            private readonly Dictionary<int, PropInfo> _info = new();
            private readonly IDatabaseMapper _databaseMapper;
            private readonly IDataReader _dataReader;

            public TestDataInfo()
            {
                Type dataType = typeof(T);
                PropertyInfo[] props = dataType.GetProperties();
                int i = 0;
                foreach (PropertyInfo prop in props)
                {
                    _info.Add(i, new PropInfo()
                    {
                        Name = prop.Name,
                        PropertyType = prop.PropertyType
                    });
                    i++;
                }
                _databaseMapper = CreateDatabaseMapper();
                _dataReader = CreateDataReader();
            }

            public int Count => _info.Count;
            public Type GetFieldType(int i) => _info[i].PropertyType;
            public string GetFieldName(int i) => _info[i].Name;
            public IDatabaseMapper DatabaseMapper => _databaseMapper;
            public IDataReader DataReader => _dataReader;

            private static IDatabaseMapper CreateDatabaseMapper()
            {
                IDatabaseMapper mapper = Substitute.For<IDatabaseMapper>();

                ConventionModelMapper modelMapper = new();
                TableInfo tableInfo = modelMapper.GetTableInfo<TestDataItem>();
                mapper.GetTableInfo<T>().Returns(tableInfo);
                mapper.GetTableInfo(typeof(T)).Returns(tableInfo);

                return mapper;
            }

            private IDataReader CreateDataReader()
            {
                IDataReader reader = Substitute.For<IDataReader>();

                reader.FieldCount.Returns(Count);
                reader.GetFieldType(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].PropertyType);
                reader.GetName(Arg.Any<int>()).Returns(ci => _info[ci.Arg<int>()].Name);
                reader.IsDBNull(Arg.Any<int>()).Returns(ci => true);

                return reader;
            }
        }

        #endregion

        [Fact]
        public void PropagateToDataObject()
        {
            TestDataInfo<TestDataItem> info = new();
            DynamicMethodModelFactory modelFactory = new(info.DatabaseMapper);
            Func<IDataReader, TestDataItem> factory = modelFactory.GetFactory<TestDataItem>(info.DataReader);

            TestDataItem actual = factory(info.DataReader);
            TestDataItem expected = new(initProperties: false);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void BeConsumedByConverters()
        {
            throw new NotImplementedException();
        }
    }
}
