using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Kros.KORM.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.Metadata
{
    public class FluentConfigurationShould
    {
        [Fact]
        public void BuildConfiguration()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<BuilderTestEntity>()
                .HasTableName("BuilderTest")
                .HasPrimaryKey(f => f.Id).AutoIncrement(AutoIncrementMethodType.Custom)
                .Property(p => p.Address)
                    .HasColumnName("COL_ADDRESS")
                    .UseConverter<AddressConverter>()
                .Property(p => p.NoMapped).NoMap()
                .Property(p => p.FirstName).HasColumnName("Name")
                .Property(p => p.DateTime).InjectValue(() => DateTime.Now)
                .Property(p => p.GeneratedValue).UseValueGeneratorOnInsert<AutoIncrementValueGenerator>();

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<BuilderTestEntity>();

            var columns = new List<ColumnInfo>() {
                new ColumnInfo()
                {
                    Name = "Id",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.Custom
                },
                new ColumnInfo() { Name = "COL_ADDRESS", Converter = new AddressConverter() },
                new ColumnInfo() { Name = "Name" },
                new ColumnInfo() { Name = "GeneratedValue", ValueGenerator = new AutoIncrementValueGenerator() }
            };
            TableInfo tableInfoExpected = CreateExpectedTableInfo(columns, "BuilderTest");

            AreSame(tableInfo, tableInfoExpected);
            modelMapper.GetInjector<BuilderTestEntity>().IsInjectable("DateTime")
                .Should()
                .BeTrue("DateTime property has injector.");
        }

        [Fact]
        public void BuildConfigurationWhenRenamePropertyForPrimaryKey()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<Foo>()
                .HasTableName("FooTable")
                .HasPrimaryKey(f => f.FooId).AutoIncrement()
                .Property(p => p.FooId).HasColumnName("RowId");

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<Foo>();

            var columns = new List<ColumnInfo>() {
                new ColumnInfo()
                {
                    Name = "RowId",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.Identity
                },
                new ColumnInfo() { Name = "Addresses" },
                new ColumnInfo() { Name = "FirstName" },
                new ColumnInfo() { Name = "NoMapped" },
                new ColumnInfo() { Name = "DateTime" }
            };
            TableInfo tableInfoExpected = CreateExpectedTableInfo(columns, "FooTable");

            AreSame(tableInfo, tableInfoExpected);
        }

        [Fact]
        public void BuildConfigurationForTwoEntities()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<Foo>()
                .HasTableName("FooTable")
                .HasPrimaryKey(f => f.FooId).AutoIncrement(AutoIncrementMethodType.Custom)
                .Property(p => p.Addresses)
                    .HasColumnName("COL_ADDRESSES")
                    .UseConverter<AddressConverter>()
                .Property(p => p.NoMapped).NoMap()
                .Property(p => p.FirstName).HasColumnName("Name")
                .Property(p => p.DateTime).InjectValue(() => DateTime.Now);

            modelBuilder.Entity<Bar>()
                .Property(p => p.FirstName)
                    .HasColumnName("GivenName")
                    .UseConverter(new UpperCaseConverter());

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<Foo>();

            var columns = new List<ColumnInfo>() {
                new ColumnInfo()
                {
                    Name = "FooId",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.Custom
                },
                new ColumnInfo() { Name = "COL_ADDRESSES", Converter = new AddressConverter() },
                new ColumnInfo() { Name = "Name" }
            };
            TableInfo tableInfoFooExpected = CreateExpectedTableInfo(columns, "FooTable");

            AreSame(tableInfo, tableInfoFooExpected);

            TableInfo tableInfoBar = modelMapper.GetTableInfo<Bar>();

            columns = new List<ColumnInfo>() {
                new ColumnInfo()
                {
                    Name = "Id",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.None
                },
                new ColumnInfo() { Name = "GivenName", Converter = new UpperCaseConverter() },
                new ColumnInfo() { Name = "LastName" }
            };
            TableInfo tableInfoBarExpected = CreateExpectedTableInfo(columns, "Bar");

            AreSame(tableInfoBar, tableInfoBarExpected);
        }

        [Fact]
        public void BuildConfigurationWhenAutoIncrementMethodTypeIsNotSet()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<Foo>()
                .HasPrimaryKey(f => f.FooId);

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<Foo>();

            var columns = new List<ColumnInfo>() {
                new ColumnInfo()
                {
                    Name = "FooId",
                    IsPrimaryKey = true,
                    AutoIncrementMethodType = AutoIncrementMethodType.None
                },
                new ColumnInfo() { Name = "Addresses" },
                new ColumnInfo() { Name = "FirstName" },
                new ColumnInfo() { Name = "NoMapped" },
                new ColumnInfo() { Name = "DateTime" }
            };
            TableInfo tableInfoExpected = CreateExpectedTableInfo(columns, "Foo");

            AreSame(tableInfo, tableInfoExpected);
        }

        [Fact]
        public void UseConverterForAllPropertiesOfSpecifiedType()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            var modelMapper = new ConventionModelMapper();

            modelBuilder.Entity<ConvertersEntity>()
                .UseConverterForProperties<int>(new IntConverter())
                .UseConverterForProperties<string, StringConverter1>()
                .Property(p => p.StringPropWithOwnConverter).UseConverter<StringConverter2>()
                .Property(p => p.StringPropWithoutConverter).IgnoreConverter();

            modelBuilder.Build(modelMapper);

            TableInfo tableInfo = modelMapper.GetTableInfo<ConvertersEntity>();

            ColumnInfo intProp1 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.IntProp1));
            ColumnInfo intProp2 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.IntProp1));
            intProp1.Converter
                .Should().BeOfType<IntConverter>()
                .And.Be(intProp2.Converter);

            ColumnInfo stringProp1 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringProp1));
            ColumnInfo stringProp2 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringProp2));
            ColumnInfo stringProp3 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringProp3));
            ColumnInfo stringProp4 = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringProp4));
            stringProp1.Converter
                .Should().BeOfType<StringConverter1>()
                .And.Be(stringProp2.Converter)
                .And.Be(stringProp3.Converter)
                .And.Be(stringProp4.Converter);

            ColumnInfo stringPropWithOwnConverter = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringPropWithOwnConverter));
            stringPropWithOwnConverter.Converter.Should().BeOfType<StringConverter2>();

            ColumnInfo stringPropWithoutConverter = tableInfo.GetColumnInfo(nameof(ConvertersEntity.StringPropWithoutConverter));
            stringPropWithoutConverter.Converter.Should().BeNull();

            tableInfo.GetColumnInfo(nameof(ConvertersEntity.BoolProp)).Converter.Should().BeNull();
            tableInfo.GetColumnInfo(nameof(ConvertersEntity.DateTimeProp)).Converter.Should().BeNull();
        }

        [Fact]
        public void ThrowExceptionWhenMappingTheTheSamePropertyMoreThanOnce()
        {
            Action builderAction = () =>
            {
                var modelBuilder = new ModelConfigurationBuilder();
                modelBuilder.Entity<ConvertersEntity>()
                    .Property(p => p.StringProp1).UseConverter<StringConverter2>()
                    .Property(p => p.StringProp1).IgnoreConverter();
            };
            builderAction.Should().Throw<InvalidOperationException>();
        }

        private static TableInfo CreateExpectedTableInfo(List<ColumnInfo> columns, string tableName)
        {
            var tableInfoExpected = new TableInfo(columns, Enumerable.Empty<PropertyInfo>(), null)
            {
                Name = tableName
            };

            return tableInfoExpected;
        }

        private void AreSame(TableInfo actual, TableInfo expected)
        {
            actual.Name.Should().Be(expected.Name);
            actual.HasIdentityPrimaryKey.Should().Be(expected.HasIdentityPrimaryKey);
            actual.Columns.Should().HaveCount(expected.Columns.Count());

            foreach (ColumnInfo columnInfo in expected.Columns)
            {
                ColumnInfo actualColumnInfo = actual.GetColumnInfo(columnInfo.Name);
                actualColumnInfo.Should().NotBeNull();
                actualColumnInfo.Name.Should().Be(columnInfo.Name);
                actualColumnInfo.IsPrimaryKey.Should().Be(columnInfo.IsPrimaryKey);
                if (columnInfo.Converter is null)
                {
                    actualColumnInfo.Converter.Should().BeNull();
                }
                else
                {
                    actualColumnInfo.Converter.Should().BeOfType(columnInfo.Converter.GetType());
                }
                if (columnInfo.ValueGenerator != null)
                {
                    actualColumnInfo.ValueGenerator.Should().BeOfType(columnInfo.ValueGenerator.GetType());
                }
            }
        }

        private class BuilderTestEntity
        {
            public int Id { get; set; }
            public string Address { get; set; }
            public string NoMapped { get; set; }
            public DateTime DateTime { get; set; }
            public string FirstName { get; set; }
            public int GeneratedValue { get; set; }
        }

        private class ConvertersEntity
        {
            public int IntProp1 { get; set; }
            public int IntProp2 { get; set; }
            public string StringProp1 { get; set; }
            public string StringProp2 { get; set; }
            public string StringProp3 { get; set; }
            public string StringProp4 { get; set; }
            public string StringPropWithOwnConverter { get; set; }
            public string StringPropWithoutConverter { get; set; }
            public bool BoolProp { get; set; }
            public DateTime DateTimeProp { get; set; }
        }

        private class StringConverter1 : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class StringConverter2 : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class IntConverter : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class Foo
        {
            public int FooId { get; set; }
            public string Addresses { get; set; }
            public string NoMapped { get; set; }
            public DateTime DateTime { get; set; }
            public string FirstName { get; set; }
        }

        private class Bar
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private class AddressConverter : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class UpperCaseConverter : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();
            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        private class AutoIncrementValueGenerator : IValueGenerator<int>
        {
            public int GetValue() => 123;

            object IValueGenerator.GetValue() => GetValue();
        }
    }
}
