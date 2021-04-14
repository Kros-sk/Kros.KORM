using FluentAssertions;
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
            IDataReader data = DataBuilder.Create(("Id", typeof(int)), ("FirstName", typeof(string)), ("Payment", typeof(string)))
                    .AddRow(22, "Foo", 120.5)
                    .Build();
            Func<IDataReader, FooWithDifferentPropertyNames> factory = GetFactory<FooWithDifferentPropertyNames>(data);

            data.Read();

            FooWithDifferentPropertyNames foo = factory(data);

            foo.Id.Should().Be(22);
            foo.Name.Should().Be("Foo");
            foo.Salary.Should().Be(120.5);
        }

        [Theory()]
        [InlineData(23, "Foo", 25.5, 1900.7, "5.4.1998", true, "{371D1F1E-57EA-4D1B-8101-3E8113AE229F}", Gender.Woman, 0.9, "30.3.2021")]
        [InlineData(26, "Bar", 27.0, null, "5.4.1998", false, "{07C39646-2929-4472-8BB2-FF0197330D24}", Gender.Woman, 1.9, "30.3.2021")]
        [InlineData(29, "FooBar", 0.5, 19000.74, "5.4.1998", true, "{0F7667BA-9795-4A32-A1FB-97D0F8353F58}", Gender.Man, 3.10, "30.3.2021")]
        [InlineData(13, "BarFoo", (double)0, 0.0, "5.4.1998", false, "{1462BD2A-3268-41AA-AB4F-C6DBD3264DB2}", Gender.Man, 20.0, "30.3.2021")]
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
            IDataReader data = DataBuilder.Create(("Id", typeof(int)), ("Name", typeof(string)), ("Age", typeof(double)),
                ("Salary", typeof(decimal?)), ("DayOfBirth", typeof(DateTime)), ("IsEmployed", typeof(bool)),
                ("TenantId", typeof(Guid)), ("Gender", typeof(Gender)), ("FloatValue", typeof(float)),
                ("ChangedDate", typeof(DateTimeOffset)))
                .AddRow(id, name, age, (decimal?)salary, dayOfBirth.ParseDateTime(), isEmployed,
                    Guid.Parse(tenantId), gender, floatValue, new DateTimeOffset(changedDate.ParseDateTime()))
                .Build();
            Func<IDataReader, FooWithDifferentTypes> factory = GetFactory<FooWithDifferentTypes>(data);

            data.Read();

            FooWithDifferentTypes bar = factory(data);

            bar.Id.Should().Be(id);
            bar.Name.Should().Be(name);
            bar.Age.Should().Be(age);
            bar.Salary.Should().Be((decimal?)salary);
            bar.DayOfBirth.Should().BeSameDateAs(dayOfBirth.ParseDateTime());
            bar.IsEmployed.Should().Be(isEmployed);
            bar.TenantId.Should().Be(tenantId);
            bar.Gender.Should().Be(gender);
            bar.FloatValue.Should().Be(floatValue);
            bar.ChangedDate.Should().BeSameDateAs(changedDate.ParseDateTime());
        }

        [Theory()]
        [InlineData(23, (int)Gender.Woman, 2356)]
        [InlineData(26, (int)Gender.Man, 4258)]
        public void ShouldReadTypeWithDefaultConversions(long id, int gender, int salary)
        {
            IDataReader data = DataBuilder.Create(("Id", typeof(long)),
                ("Gender", typeof(int)), ("Salary", typeof(int)))
                .AddRow(id, gender, salary)
                .Build();
            Func<IDataReader, FooWithDefaultConversion> factory = GetFactory<FooWithDefaultConversion>(data);

            data.Read();

            FooWithDefaultConversion bar = factory(data);

            bar.Id.Should().Be(id);
            bar.Salary.Should().Be(salary);
            bar.Gender.Should().Be(gender);
        }

        [Theory()]
        [InlineData("M", "{07C39646-2929-4472-8BB2-FF0197330D24}")]
        [InlineData("W", "{1462BD2A-3268-41AA-AB4F-C6DBD3264DB2}")]
        public void ShouldReadTypeWithCustomConverters(string gender, string tenantId)
        {
            var converter = new CustomConverter();
            IDataReader data = DataBuilder.Create(("Gender", typeof(string)), ("TenantId", typeof(Guid)))
                .AddRow(gender, new Guid(tenantId))
                .Build();
            Func<IDataReader, FooWithConverters> factory = GetFactory<FooWithConverters>(data);

            data.Read();

            FooWithConverters bar = factory(data);

            bar.TenantId.Should().BeEquivalentTo(tenantId);
            bar.Gender.Should().Be(converter.Convert(gender));
        }

        [Fact()]
        public void ShouldReadTypeWithInjectableProperty()
        {
            IDataReader data = DataBuilder.Create(("Id", typeof(long)))
                .AddRow((long)11)
                .Build();
            Func<IDataReader, FooWithInjectableProperty> factory = GetFactory<FooWithInjectableProperty>(data);

            data.Read();

            FooWithInjectableProperty bar = factory(data);

            bar.Id.Should().Be(11);
            bar.Service.Should().NotBeNull();
            bar.Service.GetValue().Should().Be(22);
        }

        [Theory()]
        [InlineData(1, 11)]
        [InlineData(2, 22)]
        public void ShouldReadTypeWithIMaterializeInterface(long id, int value)
        {
            IDataReader data = DataBuilder.Create(("Id", typeof(long)), ("Value", typeof(int)))
                .AddRow(id, value)
                .Build();
            Func<IDataReader, FooWithOnAfterMaterialize> factory = GetFactory<FooWithOnAfterMaterialize>(data);

            data.Read();

            FooWithOnAfterMaterialize bar = factory(data);

            bar.Id.Should().Be(id);
            bar.Value.Should().Be(value);
        }

        [Fact()]
        public void ShouldThrowInvalidOperationExceptionWhenCtorParameterDoesNotMatchProperty()
        {
            IDataReader data = DataBuilder.Create(("Id", typeof(long)))
                .AddRow((long)22)
                .Build();

            Action action = () =>
            {
                Func<IDataReader, FooWithDifferentPropertiesAsCtorParams> factory
                    = GetFactory<FooWithDifferentPropertiesAsCtorParams>(data);
            };

            action.Should().Throw<InvalidOperationException>().WithMessage("*'name'*'FooWithDifferentPropertiesAsCtorParams'*");
        }

        public record FooWithDifferentPropertyNames(int Id, [property: Alias("FirstName")] string Name, double Salary);

        public record FooWithDifferentTypes(int Id, string Name, double Age, decimal? Salary, DateTime DayOfBirth,
            bool IsEmployed, Guid TenantId, Gender Gender, float FloatValue, DateTimeOffset ChangedDate);

        public record FooWithDefaultConversion(long Id, Gender Gender, double Salary);

        public record FooWithConverters([property: Converter(typeof(CustomConverter))] Gender Gender, string TenantId);

        public record FooWithInjectableProperty(long Id, IService Service);

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
