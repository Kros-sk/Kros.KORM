using FluentAssertions;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
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

            modelBuilder.Entity<Foo>()
                .HasTableName("FooTable")
                .HasPrimaryKey(f => f.FooId).AutoIncrement(AutoIncrementMethodType.Custom)
                .Property(p => p.Addresses)
                    .HasColumnName("COL_ADDRESSES")
                    .UseConverter<AddressConverter>()
                .Property(p => p.NoMapped).NoMap()
                .Property(p => p.FirstName).HasColumnName("Name")
                .Property(p => p.DateTime).InjectValue(() => DateTime.Now);

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
            TableInfo tableInfoExpected = CreateExpectedTableInfo(columns, "FooTable");

            AreSame(tableInfo, tableInfoExpected);
            modelMapper.GetInjector<Foo>().IsInjectable("DateTime")
                .Should()
                .BeTrue();
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
                new ColumnInfo() { Name = "GivenName" },
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
                if (columnInfo.Converter != null)
                {
                    actualColumnInfo.Converter.Should().BeOfType(columnInfo.Converter.GetType());
                }
            }
        }

        public class Foo
        {
            public int FooId { get; set; }
            public string Addresses { get; set; }
            public string NoMapped { get; set; }
            public DateTime DateTime { get; set; }
            public string FirstName { get; set; }
        }

        public class Bar
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class AddressConverter : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();

            public object ConvertBack(object value) => throw new NotImplementedException();
        }

        public class UpperCaseConverter : IConverter
        {
            public object Convert(object value) => throw new NotImplementedException();

            public object ConvertBack(object value) => throw new NotImplementedException();
        }
    }
}
