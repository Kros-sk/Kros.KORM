using Kros.Extensions;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Injection;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Materializer
{
    public class MethodModelFactoryForRecordShould
    {
        [Fact]
        public void ShouldUseNameMapping()
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(int)), ("FirstName", typeof(string)), ("Payment", typeof(string)))
                .AddRow(22, "Foo", 120.5)
                .Build();
            Func<IDataReader, FooWithDifferentPropertyNames> factory = GetFactory<FooWithDifferentPropertyNames>(data);

            data.Read();

            FooWithDifferentPropertyNames foo = factory(data);

            Assert.Equal(22, foo.Id);
            Assert.Equal("Foo", foo.Name);
            Assert.Equal(120.5, foo.Salary);
        }

        [Theory()]
        [InlineData(23, "Foo", 25.5, 1900.7, "1998-04-05", true, "{371D1F1E-57EA-4D1B-8101-3E8113AE229F}", Gender.Woman, 0.9, "2021-03-30")]
        [InlineData(26, "Bar", 27.0, null, "1998-04-05", false, "{07C39646-2929-4472-8BB2-FF0197330D24}", Gender.Woman, 1.9, "2021-03-30")]
        [InlineData(29, "FooBar", 0.5, 19000.74, "1998-04-05", true, "{0F7667BA-9795-4A32-A1FB-97D0F8353F58}", Gender.Man, 3.10, "2021-03-30")]
        [InlineData(13, "BarFoo", (double)0, 0.0, "1998-04-05", false, "{1462BD2A-3268-41AA-AB4F-C6DBD3264DB2}", Gender.Man, 20.0, "2021-03-30")]
        public void ShouldReadDifferentTypes(
            int id,
            string name,
            double age,
            double? salary,
            string dayOfBirth,
            bool isEmployed,
            string tenantId,
            Gender gender,
            float floatValue,
            string changedDate)
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(int)), ("Name", typeof(string)), ("Age", typeof(double)),
                ("Salary", typeof(decimal)), ("DayOfBirth", typeof(DateTime)), ("IsEmployed", typeof(bool)),
                ("TenantId", typeof(Guid)), ("Gender", typeof(Gender)), ("FloatValue", typeof(float)),
                ("ChangedDate", typeof(DateTimeOffset)))
                .AddRow(id, name, age, (decimal?)salary, dayOfBirth.ParseDateTime(), isEmployed,
                    Guid.Parse(tenantId), gender, floatValue, new DateTimeOffset(changedDate.ParseDateTime()))
                .Build();
            Func<IDataReader, FooWithDifferentTypes> factory = GetFactory<FooWithDifferentTypes>(data);

            data.Read();

            FooWithDifferentTypes bar = factory(data);

            Assert.Equal(id, bar.Id);
            Assert.Equal(name, bar.Name);
            Assert.Equal(age, bar.Age);
            Assert.Equal((decimal?)salary, bar.Salary);
            Assert.Equal(dayOfBirth.ParseDateTime(), bar.DayOfBirth);
            Assert.Equal(isEmployed, bar.IsEmployed);
            Assert.Equal(new Guid(tenantId), bar.TenantId);
            Assert.Equal(gender, bar.Gender);
            Assert.Equal(floatValue, bar.FloatValue);
            Assert.Equal(changedDate.ParseDateTime(), bar.ChangedDate);
        }

        [Theory()]
        [InlineData(23, Gender.Woman, 2356)]
        [InlineData(26, Gender.Man, 4258)]
        public void ShouldReadTypeWithDefaultConversions(long id, Gender gender, int salary)
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(long)),
                ("Gender", typeof(int)), ("Salary", typeof(int)))
                .AddRow(id, gender, salary)
                .Build();
            Func<IDataReader, FooWithDefaultConversion> factory = GetFactory<FooWithDefaultConversion>(data);

            data.Read();

            FooWithDefaultConversion bar = factory(data);

            Assert.Equal(id, bar.Id);
            Assert.Equal(salary, bar.Salary);
            Assert.Equal(gender, bar.Gender);
        }

        [Theory()]
        [InlineData("M", "{07C39646-2929-4472-8BB2-FF0197330D24}")]
        [InlineData("W", "{1462BD2A-3268-41AA-AB4F-C6DBD3264DB2}")]
        public void ShouldReadTypeWithCustomConverters(string gender, string tenantId)
        {
            var converter = new CustomConverter();
            using IDataReader data = DataBuilder.Create(("Gender", typeof(string)), ("TenantId", typeof(Guid)))
                .AddRow(gender, new Guid(tenantId))
                .Build();
            Func<IDataReader, FooWithConverters> factory = GetFactory<FooWithConverters>(data);

            data.Read();

            FooWithConverters bar = factory(data);

            Assert.Equivalent(new Guid(tenantId), new Guid(bar.TenantId));
            Assert.Equal((Gender)converter.Convert(gender), bar.Gender);
        }

        [Fact()]
        public void ShouldReadTypeWithInjectableProperty()
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(long)))
                .AddRow((long)11)
                .Build();
            Func<IDataReader, FooWithInjectableProperty> factory = GetFactory<FooWithInjectableProperty>(data);

            data.Read();

            FooWithInjectableProperty bar = factory(data);

            Assert.Equal(11, bar.Id);
            Assert.NotNull(bar.Service);
            Assert.Equal(22, bar.Service.GetValue());
        }

        [Theory()]
        [InlineData(1, 11)]
        [InlineData(2, 22)]
        public void ShouldReadTypeWithIMaterializeInterface(long id, int value)
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(long)), ("Value", typeof(int)))
                .AddRow(id, value)
                .Build();
            Func<IDataReader, FooWithOnAfterMaterialize> factory = GetFactory<FooWithOnAfterMaterialize>(data);

            data.Read();

            FooWithOnAfterMaterialize bar = factory(data);

            Assert.Equal(id, bar.Id);
            Assert.Equal(value, bar.Value);
        }

        [Theory()]
        [InlineData(null, null, null, null, null, null, null, null, null, null)]
        [InlineData(12L, 'c', 32, 12.5, true, (byte)1, "2020-02-06", 56.7, "{3D6F4D25-60E8-432B-B6A7-3ADDBD331812}", (float)58.9)]
        [InlineData(52L, '*', 2, 18.05, false, (byte)0, "2028-09-08", 152.007, "{94FB4F1C-9FEE-457C-84E8-E7562601DC39}", (float)8)]
        public void ShouldReadTypeWithNullableTypes(long? longValue, char? charValue, int? intValue, double? decimalValue,
            bool? boolValue, byte? byteValue, string dateTimeValue, double? doubleValue,
            string guidValue, float? floatValue)
        {
            DateTime? dt = dateTimeValue.IsNullOrEmpty() ? null : dateTimeValue.ParseDateTime();
            Guid? guid = guidValue.IsNullOrEmpty() ? null : new Guid(guidValue);

            using IDataReader data = DataBuilder.Create(("LongValue", typeof(long)), ("CharValue", typeof(char)),
                ("IntValue", typeof(int)), ("DecimalValue", typeof(decimal)), ("BoolValue", typeof(bool)),
                ("ByteValue", typeof(byte)), ("DateTimeValue", typeof(DateTime)), ("DoubleValue", typeof(double)),
                ("GuidValue", typeof(Guid)), ("FloatValue", typeof(float)))
                .AddRow(longValue, charValue, intValue, (decimal?)decimalValue, boolValue, byteValue, dt, doubleValue, guid, floatValue)
                .Build();
            Func<IDataReader, FooWithNullableTypes> factory = GetFactory<FooWithNullableTypes>(data);

            data.Read();

            FooWithNullableTypes bar = factory(data);

            Assert.Equal(longValue, bar.LongValue);
            Assert.Equal(charValue, bar.CharValue);
            Assert.Equal(intValue, bar.IntValue);
            Assert.Equal((decimal?)decimalValue, bar.DecimalValue);
            Assert.Equal(boolValue, bar.BoolValue);
            Assert.Equal(byteValue, bar.ByteValue);
            Assert.Equal(dt, bar.DateTimeValue);
            Assert.Equal(doubleValue, bar.DoubleValue);
            Assert.Equal(guid, bar.GuidValue);
            Assert.Equal(floatValue, bar.FloatValue);
        }

        [Fact()]
        public void ShouldThrowInvalidOperationExceptionWhenCtorParameterDoesNotMatchProperty()
        {
            using IDataReader data = DataBuilder.Create(("Id", typeof(long)))
                .AddRow((long)22)
                .Build();

            Action action = () =>
            {
                Func<IDataReader, FooWithDifferentPropertiesAsCtorParams> factory
                    = GetFactory<FooWithDifferentPropertiesAsCtorParams>(data);
            };

            var ex = Assert.Throws<InvalidOperationException>(action);
            Assert.Matches(@".*'name'.*'" + System.Text.RegularExpressions.Regex.Escape(typeof(FooWithDifferentPropertiesAsCtorParams).FullName) + @"'.*", ex.Message);
        }

        public record FooWithDifferentPropertyNames(int Id, [property: Alias("FirstName")] string Name, double Salary);

        public record FooWithDifferentTypes(int Id, string Name, double Age, decimal? Salary, DateTime DayOfBirth,
            bool IsEmployed, Guid TenantId, Gender Gender, float FloatValue, DateTimeOffset ChangedDate);

        public record FooWithDefaultConversion(long Id, Gender Gender, double Salary);

        public record FooWithConverters([property: Converter(typeof(CustomConverter))] Gender Gender, string TenantId);

        public record FooWithInjectableProperty(long Id, IService Service);

        public record FooWithNullableTypes(
            long? LongValue, char? CharValue, int? IntValue, decimal? DecimalValue,
            bool? BoolValue, byte? ByteValue, DateTime? DateTimeValue, double? DoubleValue,
            Guid? GuidValue, float? FloatValue);

        public record FooWithOnAfterMaterialize(long Id) : IMaterialize
        {
            public int Value { get; set; }

            public void OnAfterMaterialize(IDataRecord source)
                => Value = source.GetInt32(source.GetOrdinal("Value"));
        }

        public class FooWithDifferentPropertiesAsCtorParams
        {
            private string _name;

            public FooWithDifferentPropertiesAsCtorParams(long id, string name)
            {
                Id = id;
                _name = name;
            }

            public long Id { get; set; }
        }

        public interface IService
        {
            int GetValue();
        }

        public class Service : IService
        {
            public int GetValue() => 22;
        }

        public class CustomConverter : IConverter
        {
            public object Convert(object value) => value.ToString() switch
            {
                "M" => Gender.Man,
                "V" => Gender.Woman,
                _ => Gender.None
            };

            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        public enum Gender
        {
            None,
            Man,
            Woman
        }

        #region Helpers

        private static Func<IDataReader, T> GetFactory<T>(IDataReader dataReader)
        {
            (TableInfo tableInfo, IInjector injector) = GetTableInfo<T>();

            (ConstructorInfo ctor, bool _) = typeof(T).GetConstructor();

            return RecordModelFactory.CreateFactoryForRecords<T>(dataReader, tableInfo, injector, ctor);
        }

        private static (TableInfo table, IInjector injector) GetTableInfo<T>()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<FooWithDifferentPropertyNames>()
                .Property(p => p.Salary).HasColumnName("Payment");

            modelBuilder.Entity<FooWithConverters>()
                .Property(p => p.TenantId).UseConverter<GuidToStringConverter>();

            modelBuilder.Entity<FooWithInjectableProperty>()
                .Property(p => p.Service).InjectValue(() => new Service());

            modelBuilder.Build(modelMapper);

            return (modelMapper.GetTableInfo<T>(), modelMapper.GetInjector<T>());
        }

        private class DataBuilder
        {
            private readonly (string name, Type type)[] _names;
            private readonly List<object[]> _values = new List<object[]>();

            public DataBuilder(params (string name, Type type)[] names)
            {
                _names = names;
            }

            public DataBuilder AddRow(params object[] values)
            {
                _values.Add(values);
                return this;
            }

            public IDataReader Build()
            {
                var data = new List<Dictionary<string, object>>();
                foreach (object[] values in _values)
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < _names.Length; i++)
                    {
                        row.Add(_names[i].name, values[i]);
                    }
                    data.Add(row);
                }

                return new InMemoryDataReader(data, _names.Select(p => p.type));
            }

            public static DataBuilder Create(params (string name, Type type)[] names)
                => new DataBuilder(names);
        }

        #endregion
    }
}
