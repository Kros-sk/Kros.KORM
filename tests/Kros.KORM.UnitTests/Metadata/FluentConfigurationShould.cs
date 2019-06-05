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

            var tableInfo = modelMapper.GetTableInfo<Foo>();

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
            var tableInfoExpected = CreateExpectedTableInfo(columns, "FooTable");

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

            var tableInfo = modelMapper.GetTableInfo<Foo>();

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
            var tableInfoExpected = CreateExpectedTableInfo(columns, "FooTable");

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

            var tableInfo = modelMapper.GetTableInfo<Foo>();

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
            var tableInfoFooExpected = CreateExpectedTableInfo(columns, "FooTable");

            AreSame(tableInfo, tableInfoFooExpected);

            var tableInfoBar = modelMapper.GetTableInfo<Bar>();

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
            var tableInfoBarExpected = CreateExpectedTableInfo(columns, "Bar");

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

            var tableInfo = modelMapper.GetTableInfo<Foo>();

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
            var tableInfoExpected = CreateExpectedTableInfo(columns, "Foo");

            AreSame(tableInfo, tableInfoExpected);
        }

        [Fact]
        public void ThrowExceptionIfTrySetTableNameSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            modelBuilder.Entity<Foo>()
                .HasTableName("FooTable");

            Action action = () => modelBuilder.Entity<Foo>().HasTableName("FooTable");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'HasTableName'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetPrimaryKeySecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            modelBuilder.Entity<Foo>()
                .HasPrimaryKey(p => p.FooId);

            Action action = () => modelBuilder.Entity<Foo>().HasPrimaryKey(p => p.FooId);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'HasPrimaryKey'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetAutoIncrementSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PrimaryKeyBuilder<Foo> primaryKey =
                modelBuilder.Entity<Foo>()
                    .HasPrimaryKey(p => p.FooId).AutoIncrement();

            Action action = () => primaryKey.AutoIncrement();

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'AutoIncrement'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetColumnNameSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.FirstName).HasColumnName("Name");

            Action action = () => property.HasColumnName("Name1");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'HasColumnName'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetNoMapSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.FirstName).NoMap();

            Action action = () => property.NoMap();

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'NoMap'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetConverterSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.Addresses).UseConverter<AddressConverter>();

            Action action = () => property.UseConverter(new AddressConverter());

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'UseConverter'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetInjectorSecondTime()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.DateTime).InjectValue(() => DateTime.Now);

            Action action = () => property.InjectValue(() => DateTime.Now);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call multiple time method 'InjectValue'.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetInjectorWhenColumnNameWasSet()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.DateTime).HasColumnName("Date");

            Action action = () => property.InjectValue(() => DateTime.Now);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call 'InjectValue' if you configured column name or converter.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetInjectorWhenConverterSet()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.Addresses).UseConverter<AddressConverter>();

            Action action = () => property.InjectValue(() => string.Empty);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Can't call 'InjectValue' if you configured column name or converter.");
        }

        [Fact]
        public void ThrowExceptionIfTrySetAnythingElseAfterNoMap()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.Addresses).NoMap();

            Action action = () => property.HasColumnName("Col1");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");

            action = () => property.UseConverter<AddressConverter>();

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");
        }

        [Fact]
        public void ThrowExceptionIfTrySetAnythingElseAfterInjector()
        {
            var modelBuilder = new ModelConfigurationBuilder();
            PropertyBuilder<Foo> property =
                modelBuilder.Entity<Foo>()
                    .Property(p => p.Addresses).InjectValue(() => string.Empty);

            Action action = () => property.HasColumnName("Col1");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");

            action = () => property.UseConverter<AddressConverter>();

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("You cannot configure anything else if you are call*");
        }

        private static TableInfo CreateExpectedTableInfo(List<ColumnInfo> columns, string tableName)
        {
            var tableInfoExpected = new TableInfo(columns, Enumerable.Empty<PropertyInfo>(), null);
            tableInfoExpected.Name = tableName;

            return tableInfoExpected;
        }

        private void AreSame(TableInfo actual, TableInfo expected)
        {
            actual.Name.Should().Be(expected.Name);
            actual.HasIdentityPrimaryKey.Should().Be(expected.HasIdentityPrimaryKey);
            actual.Columns.Should().HaveCount(expected.Columns.Count());

            foreach (var columnInfo in expected.Columns)
            {
                var actualColumnInfo = actual.GetColumnInfo(columnInfo.Name);
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
            public object Convert(object value)
            {
                throw new NotImplementedException();
            }

            public object ConvertBack(object value)
            {
                throw new NotImplementedException();
            }
        }

        public class UpperCaseConverter : IConverter
        {
            public object Convert(object value)
            {
                throw new NotImplementedException();
            }

            public object ConvertBack(object value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
