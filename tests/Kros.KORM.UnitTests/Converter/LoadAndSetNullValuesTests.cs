using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.UnitTests.Base;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Converter
{
    public class LoadAndSetNullValuesTests : DatabaseTestBase
    {
        #region Database tests

        #region Database schema

        private const string TableName = "DbNullValues";

        private static readonly string CreateTableScript =
$@"CREATE TABLE [dbo].[{TableName}] (
    [Id] [int] NOT NULL,
    [Data] [nvarchar](250) NULL,
) ON [PRIMARY];";

        private static readonly string InsertDataScript =
$@"INSERT INTO [{TableName}] VALUES (1, '[]');
INSERT INTO [{TableName}] VALUES (2, '[""lorem""]');
INSERT INTO [{TableName}] VALUES (3, '[""lorem"", ""ipsum""]');
INSERT INTO [{TableName}] VALUES (4, '');
INSERT INTO [{TableName}] VALUES (5, NULL);
INSERT INTO [{TableName}] VALUES (10, 'Ipsum');
INSERT INTO [{TableName}] VALUES (11, '');
INSERT INTO [{TableName}] VALUES (12, NULL);";

        #endregion

        #region Helpers

        private class DataItemWithConverter
        {
            public int Id { get; set; }
            public List<string> Data { get; set; }
        }

        private class DataItemWithDefaultValue
        {
            public DataItemWithDefaultValue()
            {
                Id = -1;
                Data = "Lorem";
            }

            public int Id { get; set; }
            public string Data { get; set; }
        }

        private class JsonToListConverter<T> : IConverter
        {
            public object Convert(object value)
            {
                string json = (string)value;
                return string.IsNullOrEmpty(json)
                    ? new List<T>()
                    : JsonConvert.DeserializeObject<List<T>>(json);
            }

            public object ConvertBack(object value) => JsonConvert.SerializeObject(value);
        }

        private class DatabaseConfiguration : DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                modelBuilder.Entity<DataItemWithConverter>()
                    .HasTableName(TableName)
                    .UseConverterForProperties<string>(NullAndTrimStringConverter.ConvertNullAndTrimString)
                    .Property(u => u.Data).UseConverter<JsonToListConverter<string>>();
                modelBuilder.Entity<DataItemWithDefaultValue>()
                    .HasTableName(TableName);
            }
        }

        private static IDatabase CreateDatabaseWithConfiguration(TestDatabase sourceDb)
        {
            IDatabase db = Database.Builder
                .UseConnection(sourceDb.ConnectionString)
                .UseDatabaseConfiguration<DatabaseConfiguration>()
                .Build();
            return db;
        }

        #endregion

        [Theory]
        [MemberData(nameof(SetNullValueFromDatabase_Data))]
        public void SetNullValueFromDatabase(int id, string expectedData)
        {
            using TestDatabase testDb = CreateDatabase(new[] { CreateTableScript, InsertDataScript });
            using IDatabase db = CreateDatabaseWithConfiguration(testDb);

            DataItemWithDefaultValue expectedItem = new()
            {
                Id = id,
                Data = expectedData
            };
            DataItemWithDefaultValue actualItem = db.Query<DataItemWithDefaultValue>().Where(item => item.Id == id).Single();

            actualItem.Should().BeEquivalentTo(expectedItem);
        }

        public static IEnumerable<object[]> UseConverterForNullValue_Data()
        {
            yield return new object[] { 1, new List<string>() };
            yield return new object[] { 2, new List<string>(new[] { "lorem" }) };
            yield return new object[] { 3, new List<string>(new[] { "lorem", "ipsum" }) };
            yield return new object[] { 4, new List<string>() };
            yield return new object[] { 5, new List<string>() };
        }

        [Theory]
        [MemberData(nameof(UseConverterForNullValue_Data))]
        public void UseConverterForNullValue(int id, List<string> expectedData)
        {
            using TestDatabase testDb = CreateDatabase(new[] { CreateTableScript, InsertDataScript });
            using IDatabase db = CreateDatabaseWithConfiguration(testDb);

            DataItemWithConverter expectedItem = new()
            {
                Id = id,
                Data = expectedData
            };
            DataItemWithConverter actualItem = db.Query<DataItemWithConverter>().Where(item => item.Id == id).Single();

            actualItem.Should().BeEquivalentTo(expectedItem);
        }

        public static IEnumerable<object[]> SetNullValueFromDatabase_Data()
        {
            yield return new object[] { 10, "Ipsum" };
            yield return new object[] { 11, "" };
            yield return new object[] { 12, null };
        }

        #endregion

        #region No database tests

        private class PropInfo
        {
            public string Name { get; set; }
            public Type PropertyType { get; set; }
        }

        private class DataInfo<T>
        {
            private readonly Dictionary<string, int> _nameMap = new();
            private readonly Dictionary<int, PropInfo> _info = new();
            private readonly IDatabaseMapper _databaseMapper;
            private readonly IDataReader _dataReader;

            public DataInfo()
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
                    _nameMap.Add(prop.Name, i);
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
                TableInfo tableInfo = modelMapper.GetTableInfo<TestItem>();
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
                reader.IsDBNull(Arg.Any<int>()).Returns(ci =>
                {
                    Console.WriteLine("IDataReader.IsDBNull");
                    return true;
                });

                return reader;
            }
        }

        private class TestSubItem
        {
        }

        private struct TestStruct
        {
            public int IntVal;
            public string StringVal;
        }

        private class TestItem
        {
            public TestItem() : this(true)
            {
            }

            public TestItem(bool initValuesInConstructor)
            {
                if (initValuesInConstructor)
                {
                    ObjectVal = new TestSubItem();
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
                    CustomStructVal = new TestStruct() { IntVal = 42, StringVal = "Lorem" };
                    NullableCustomStructVal = new TestStruct() { IntVal = 42, StringVal = "Lorem" };
                }
            }

            public TestSubItem ObjectVal { get; set; }
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
            public TestStruct CustomStructVal { get; set; }
            public TestStruct? NullableCustomStructVal { get; set; }
        }

        [Fact]
        public void Test()
        {
            DataInfo<TestItem> info = new();
            DynamicMethodModelFactory modelFactory = new DynamicMethodModelFactory(info.DatabaseMapper);
            Func<IDataReader, TestItem> factory = modelFactory.GetFactory<TestItem>(info.DataReader);

            TestItem actual = factory(info.DataReader);
            TestItem expected = new(false);
            actual.Should().BeEquivalentTo(expected);
        }

        #endregion
    }
}
