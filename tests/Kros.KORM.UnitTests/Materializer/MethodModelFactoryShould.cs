using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Injection;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Helper;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Materializer
{
    public class MethodModelFactoryShould
    {
        #region Tests

        [Fact]
        public void CreateFactoryWhichKnowFillingObjectsWithPrimitiveTypes()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            IDataReader data = new InMemoryDataReader(CreateData());
            data.Read();

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.Id.Should().Be(1);
            foo.PropertyString.Should().Be("Hello");
            foo.PropertyDouble.Should().Be(45.78);
            foo.PropertyDecimal.Should().Be(785.78M);
            foo.PropertyDateTime.Should().Be(new DateTime(1980, 7, 24));
            foo.DateTimeOffset.Should().Be(new DateTimeOffset(1985, 9, 20, 10, 11, 22, 123, TimeSpan.FromHours(5)));
            foo.DateTimeOffsetNullable.Should().Be(new DateTimeOffset(1985, 9, 20, 10, 11, 22, 123, TimeSpan.FromHours(5)));
            foo.Float.Should().Be(12.8F);
            foo.FloatNullable.Should().Be(45.89F);
            foo.Is.Should().BeTrue();
            foo.PropertyGuid.Should().Be(new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"));
        }

        [Fact]
        public void CreateFactoryWhichKnowFillingObjectsWithNullValues()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();
            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            data.CurrentValues[1] = DBNull.Value;
            data.CurrentValues[2] = DBNull.Value;
            data.CurrentValues[6] = DBNull.Value;

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.PropertyString.Should().BeNull();
            foo.PropertyDouble.Should().Be(0);
            foo.PropertyGuid.Should().BeEmpty();
        }

        [Fact]
        public void CreateFactoryWhichKnowFillingObjectsWithEnums()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();

            rows[0].Add("PropertyEnum", 3);
            IDataReader data = new InMemoryDataReader(rows);
            data.Read();
            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.PropertyEnum.Should().Be(TestEnum.Value3);
        }

        [Fact]
        public void CreateFactoryWhichUseConverter()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();

            rows[0].Add("PropertyEnumConv", "V2");
            IDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.PropertyEnumConv.Should().Be(TestEnum.Value2);
        }

        [Fact]
        public void CreateFactoryWhichUseStandardTypeConverter()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();
            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            data.CurrentTypes[0] = typeof(double);
            data.CurrentValues[0] = 25.45;

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.Id.Should().Be(25);
        }

        [Fact]
        public void CreateFactoryWhichConvertNullableType()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();
            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.Age.Should().Be(18.5);
        }

        [Fact]
        public void IgnoreNonMapFields()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();

            rows[0].Add("Bar", 3);
            IDataReader data = new InMemoryDataReader(rows);
            data.Read();
            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.Bar.Should().Be(0);
        }

        [Fact]
        public void IgnoreNonExistingProperties()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();

            rows[0].Add("XXX", 3);
            IDataReader data = new InMemoryDataReader(rows);
            data.Read();
            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);
        }

        [Fact]
        public void NonNullableTypeMaterializeToNullable()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var rows = CreateData();

            rows[0].Add("PropertyDateTimeNullable", new DateTime(2005, 1, 5));

            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            data.CurrentTypes[7] = typeof(DateTime);

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);
            foo.PropertyDateTimeNullable.Should().Be(new DateTime(2005, 1, 5));
        }

        [Fact]
        public void MaterializeValueType()
        {
            DynamicMethodModelFactory factory = CreateFactory();
            var reader = new ValueTypesReader<int>(new List<int>() { 2, 5, 9, 15, 3 });

            var actual = new List<int>();

            while (reader.Read())
            {
                var fact = factory.GetFactory<int>(reader);
                actual.Add(fact(reader));
            }

            actual.Should().BeEquivalentTo(new List<int>() { 2, 5, 9, 15, 3 });
        }

        [Fact]
        public void CallOnAfterMaterializeWhenModelImplementIMaterialize()
        {
            var modelMapper = CreateModelMapperForBar();
            DynamicMethodModelFactory factory = new DynamicMethodModelFactory(new DatabaseMapper(modelMapper));

            var rows = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            };

            rows[0].Add("Prop1", 2);
            rows[0].Add("Prop2", 1);
            rows[0].Add("Prop4", 111);

            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Bar>(data);

            var bar = fact(data);
            bar.Prop3.Should().Be(6);
            bar.Prop4.Should().Be(111);
        }

        [Fact]
        public void MaterializeByteArray()
        {
            var modelMapper = CreateModelMapperForBar();
            DynamicMethodModelFactory factory = new DynamicMethodModelFactory(new DatabaseMapper(modelMapper));

            var rows = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            };
            var image = new byte[] { 0x20, 0x20, 0x20 };

            rows[0].Add("Prop1", 2);
            rows[0].Add("Prop2", 1);
            rows[0].Add("Prop4", 111);
            rows[0].Add("Image", image);

            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Bar>(data);

            var bar = fact(data);
            bar.Image.Should().BeEquivalentTo(image);
        }

        [Fact]
        public void MaterializeByteArrayInvalidCast()
        {
            var modelMapper = CreateModelMapperForBar();
            DynamicMethodModelFactory factory = new DynamicMethodModelFactory(new DatabaseMapper(modelMapper));

            var rows = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            };
            var image = new byte[] { 0x20, 0x20, 0x20 };

            rows[0].Add("Prop1", 2);
            rows[0].Add("Prop2", 1);
            rows[0].Add("Prop4", image); // Bar.Prop4 => integer
            rows[0].Add("Image", image);

            InMemoryDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Bar>(data);
            Bar bar;

            Action act = () => bar = fact(data);

            act.Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void InjectServiceToProperty()
        {
            var service = new TestService();
            var factory = CreateFactory(CreateModelMapperWithInjection(service));

            var rows = CreateData();

            rows[0].Add("XXX", 3);
            IDataReader data = new InMemoryDataReader(rows);
            data.Read();

            var fact = factory.GetFactory<Foo>(data);

            var foo = fact(data);

            foo.Service.Should().Be(service);

            data.Read();
            foo = fact(data);
            foo.Service.Should().Be(service);
        }

        #endregion

        #region Helpers

        private IModelMapper CreateModelMapperWithInjection(TestService service)
        {
            var modelMapper = Substitute.For<IModelMapper>();

            IModelMapper mapper = new ConventionModelMapper();
            var injecter = mapper.InjectionConfigurator<Foo>()
                .FillProperty(p => p.Service, () => service);

            var tableInfo = CreateTableInfo();
            modelMapper.GetTableInfo<Foo>().Returns(tableInfo);
            modelMapper.GetTableInfo(Arg.Any<Type>()).Returns(tableInfo);
            modelMapper.GetInjector<Foo>().Returns(injecter as IInjector);

            return modelMapper;
        }

        private DynamicMethodModelFactory CreateFactory()
        {
            return CreateFactory(CreateModelMapper(CreateTableInfo()));
        }

        private DynamicMethodModelFactory CreateFactory(IModelMapper modelMapper)
        {
            DynamicMethodModelFactory factory = new DynamicMethodModelFactory(new DatabaseMapper(modelMapper));

            return factory;
        }

        private IModelMapper CreateModelMapper(TableInfo tableInfo)
        {
            var modelMapper = Substitute.For<IModelMapper>();

            modelMapper.GetTableInfo<Foo>().Returns(tableInfo);
            modelMapper.GetTableInfo(Arg.Any<Type>()).Returns(tableInfo);

            return modelMapper;
        }

        private TableInfo CreateTableInfo()
        {
            List<ColumnInfo> columns = new List<ColumnInfo>() {
                new ColumnInfo(){ Name = "Id", PropertyInfo = GetPropertyInfo<Foo>("Id"), IsPrimaryKey = true },
                new ColumnInfo(){ Name = "FirstName", PropertyInfo = GetPropertyInfo<Foo>("PropertyString")},
                new ColumnInfo(){ Name = "Something", PropertyInfo = GetPropertyInfo<Foo>("PropertyDouble")},
                new ColumnInfo(){ Name = "Salary", PropertyInfo = GetPropertyInfo<Foo>("PropertyDecimal")},
                new ColumnInfo(){ Name = "Birthday", PropertyInfo = GetPropertyInfo<Foo>("PropertyDateTime")},
                new ColumnInfo(){ Name = "Is", PropertyInfo = GetPropertyInfo<Foo>("Is")},
                new ColumnInfo(){ Name = "PropertyGuid", PropertyInfo = GetPropertyInfo<Foo>("PropertyGuid")},
                new ColumnInfo(){ Name = "PropertyEnum", PropertyInfo = GetPropertyInfo<Foo>("PropertyEnum")},
                new ColumnInfo(){ Name = "PropertyDateTimeNullable", PropertyInfo = GetPropertyInfo<Foo>("PropertyDateTimeNullable")},
                new ColumnInfo(){ Name = "PropertyEnumConv", PropertyInfo = GetPropertyInfo<Foo>("PropertyEnumConv"), Converter = new TestEnumConverter()},
                new ColumnInfo(){ Name = "Age", PropertyInfo = GetPropertyInfo<Foo>("Age")},
                new ColumnInfo(){ Name = "Float", PropertyInfo = GetPropertyInfo<Foo>("Float")},
                new ColumnInfo(){ Name = "FloatNullable", PropertyInfo = GetPropertyInfo<Foo>("FloatNullable")},
                new ColumnInfo(){ Name = "DateTimeOffset", PropertyInfo = GetPropertyInfo<Foo>("DateTimeOffset")},
                new ColumnInfo(){ Name = "DateTimeOffsetNullable", PropertyInfo = GetPropertyInfo<Foo>("DateTimeOffsetNullable")},
                new ColumnInfo(){ Name = "Service", PropertyInfo = GetPropertyInfo<Foo>("Service")}
            };

            TableInfo tableInfo = new TableInfo(columns, columns.Select(p => p.PropertyInfo), null);
            return tableInfo;
        }

        private PropertyInfo GetPropertyInfo<T>(string propertyName)
        {
            return typeof(T).GetProperty(propertyName);
        }

        private List<Dictionary<string, object>> CreateData()
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();

            AddRow(ret, 1, "Hello", 45.78, (decimal)785.78, new DateTime(1980, 7, 24),
                true, new Guid("ddc995d7-4dda-41ca-abab-7f45e651784a"), 18.5F,
                floatValue: 12.8F,
                floatNullableValue: 45.89F,
                dateTimeOffset: new DateTimeOffset(1985, 9, 20, 10, 11, 22, 123, TimeSpan.FromHours(5)),
                dateTimeOffsetNullable: new DateTimeOffset(1985, 9, 20, 10, 11, 22, 123, TimeSpan.FromHours(5)));

            return ret;
        }

        private static void AddRow(List<Dictionary<string, object>> ret,
                                                                int id,
                                                             string firstName,
                                                             double something,
                                                            decimal salary,
                                                           DateTime birthday,
                                                               bool iS,
                                                               Guid guid,
                                                            Single? age,
                                                            float floatValue,
                                                            float? floatNullableValue,
                                                            DateTimeOffset dateTimeOffset,
                                                            DateTimeOffset? dateTimeOffsetNullable)
        {
            Dictionary<string, object> row = new Dictionary<string, object>() { { "Id", id },
                                                                                { "FirstName", firstName },
                                                                                { "Something", something},
                                                                                { "Salary",salary},
                                                                                { "Birthday", birthday},
                                                                                { "Is", iS},
                                                                                { "PropertyGuid", guid},
                                                                                { "Age", age},
                                                                                { "Float", floatValue},
                                                                                { "FloatNullable", floatNullableValue},
                                                                                { "DateTimeOffset", dateTimeOffset},
                                                                                { "DateTimeOffsetNullable", dateTimeOffsetNullable}};

            ret.Add(row);
        }

        private IModelMapper CreateModelMapperForBar()
        {
            var modelMapper = Substitute.For<IModelMapper>();

            List<ColumnInfo> columns = new List<ColumnInfo>() {
                new ColumnInfo(){ Name = "Prop1", PropertyInfo = GetPropertyInfo<Bar>("Prop1"), IsPrimaryKey = true },
                new ColumnInfo(){ Name = "Prop2", PropertyInfo = GetPropertyInfo<Bar>("Prop2")},
                new ColumnInfo(){ Name = "Prop4", PropertyInfo = GetPropertyInfo<Bar>("Prop4")},
                new ColumnInfo(){ Name = "Image", PropertyInfo = GetPropertyInfo<Bar>("Image")}
            };

            TableInfo tableInfo = new TableInfo(columns, columns.Select(p => p.PropertyInfo), typeof(Bar).GetMethod("OnAfterMaterialize"));

            modelMapper.GetTableInfo<Bar>().Returns(tableInfo);
            modelMapper.GetTableInfo(Arg.Any<Type>()).Returns(tableInfo);

            return modelMapper;
        }

        #endregion

        #region Test classes

        private class Foo
        {
            public int Id { get; set; }

            [Alias("FirstName")]
            public string PropertyString { get; set; }

            [Alias("Something")]
            public double PropertyDouble { get; set; }

            [Alias("Salary")]
            public decimal PropertyDecimal { get; set; }

            [Alias("Birthday")]
            public DateTime PropertyDateTime { get; set; }

            public DateTime? PropertyDateTimeNullable { get; set; }

            public bool Is { get; set; }

            [NoMap]
            public int Bar { get; set; }

            public Guid PropertyGuid { get; set; }

            public TestEnum PropertyEnum { get; set; }

            [Converter(typeof(TestEnumConverter))]
            public TestEnum PropertyEnumConv { get; set; }

            public double? Age { get; set; }

            public float Float { get; set; }

            public float? FloatNullable { get; set; }

            public DateTimeOffset DateTimeOffset { get; set; }

            public DateTimeOffset? DateTimeOffsetNullable { get; set; }

            [NoMap()]
            public TestService Service { get; set; }
        }

        public class TestService
        {
        }

        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3,
        }

        private class TestEnumConverter : IConverter
        {
            public object Convert(object value)
            {
                var val = value.ToString();

                if (val == "V1")
                {
                    return TestEnum.Value1;
                }
                else if (val == "V2")
                {
                    return TestEnum.Value2;
                }
                else
                {
                    return TestEnum.Value3;
                }
            }

            public object ConvertBack(object value)
            {
                throw new NotImplementedException();
            }
        }

        private class Bar : IMaterialize
        {
            public int Prop1 { get; set; }

            public int Prop2 { get; set; }

            [NoMap]
            public int Prop3 { get; set; }

            [NoMap]
            public int Prop4 { get; set; }

            public byte[] Image { get; set; }

            public void OnAfterMaterialize(IDataRecord source)
            {
                Prop3 = 2 * (Prop1 + Prop2);
                Prop4 = source.GetInt32(source.GetOrdinal("Prop4"));
            }
        }

        private class ValueTypesReader<T> : IDataReader
        {
            private readonly IEnumerator<T> _values;

            public ValueTypesReader(IEnumerable<T> values)
            {
                this._values = values.GetEnumerator();
            }

            public object this[int i] => _values.Current;

            public object this[string name] => throw new NotImplementedException();

            public int Depth => throw new NotImplementedException();

            public bool IsClosed => throw new NotImplementedException();

            public int RecordsAffected => throw new NotImplementedException();

            public int FieldCount => 1;

            public void Close()
            {
            }

            public void Dispose()
            {
            }

            public bool GetBoolean(int i) => (bool)this[i];

            public byte GetByte(int i) => (byte)this[i];

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public char GetChar(int i)
            {
                throw new NotImplementedException();
            }

            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public IDataReader GetData(int i)
            {
                throw new NotImplementedException();
            }

            public string GetDataTypeName(int i)
            {
                throw new NotImplementedException();
            }

            public DateTime GetDateTime(int i) => (DateTime)this[i];

            public decimal GetDecimal(int i) => (decimal)this[i];

            public double GetDouble(int i) => (double)this[i];

            public Type GetFieldType(int i) => typeof(T);

            public float GetFloat(int i) => (float)this[i];

            public Guid GetGuid(int i)
            {
                throw new NotImplementedException();
            }

            public short GetInt16(int i)
            {
                throw new NotImplementedException();
            }

            public int GetInt32(int i) => (int)this[i];

            public long GetInt64(int i)
            {
                throw new NotImplementedException();
            }

            public string GetName(int i) => "Id";

            public int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public DataTable GetSchemaTable()
            {
                throw new NotImplementedException();
            }

            public string GetString(int i) => (string)this[i];

            public object GetValue(int i) => this[i];

            public int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public bool IsDBNull(int i)
            {
                throw new NotImplementedException();
            }

            public bool NextResult()
            {
                throw new NotImplementedException();
            }

            public bool Read() => _values.MoveNext();
        }

        #endregion
    }
}
