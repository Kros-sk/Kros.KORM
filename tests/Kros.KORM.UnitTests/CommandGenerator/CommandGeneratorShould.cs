﻿using FluentAssertions;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.Providers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kros.KORM.UnitTests.CommandGenerator
{
    public class CommandGeneratorShould
    {
        #region Tests

        [Fact]
        public void HaveCorrectInsertCommandText()
        {
            const string expectedQuery = "INSERT INTO [Foo] ([IdRow], [Salary]) VALUES (@IdRow, @Salary)";

            DbCommand insert = GetFooGenerator().GetInsertCommand();

            insert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectInsertCommandTextWhenTableHaveIdentityPrimaryKey()
        {
            const string expectedQuery = "INSERT INTO [FooIdentity] ([Salary]) OUTPUT INSERTED.IdRow VALUES (@Salary)";

            DbCommand insert = GetFooIdentityGenerator().GetInsertCommand();

            insert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectUpdateCommandText()
        {
            const string expectedQuery = "UPDATE [Foo] SET [Salary] = @Salary WHERE ([IdRow] = @IdRow)";

            DbCommand update = GetFooGenerator().GetUpdateCommand();

            update.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectDeleteCommandText()
        {
            const string expectedQuery = "DELETE FROM [Foo] WHERE ([IdRow] = @IdRow)";

            DbCommand delete = GetFooGenerator().GetDeleteCommand();

            delete.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectOneDeleteQuery()
        {
            const string expectedQuery = "DELETE FROM [Foo] WHERE [IdRow] IN (@P1,@P2,@P3,@P4,@P5,@P6,@P7,@P8,@P9,@P10,@P11,@P12,@P13,@P14,@P15)";

            var result = GetFooGenerator().GetDeleteCommands(GetFooList(15)).ToList();

            result.Should().HaveCount(1);
            result[0].CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectThreeDeleteQueries()
        {
            const string expectedQuery_0 = "DELETE FROM [Foo] WHERE [IdRow] IN (@P1,@P2,@P3,@P4,@P5,@P6,@P7,@P8,@P9,@P10)";
            const string expectedQuery_1 = "DELETE FROM [Foo] WHERE [IdRow] IN (@P1,@P2,@P3,@P4,@P5,@P6,@P7,@P8,@P9,@P10)";
            const string expectedQuery_2 = "DELETE FROM [Foo] WHERE [IdRow] IN (@P1,@P2,@P3,@P4,@P5)";
            int[] expectedParameters_0 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int[] expectedParameters_1 = { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            int[] expectedParameters_2 = { 21, 22, 23, 24, 25 };

            CommandGenerator<Foo> generator = GetFooGenerator();
            generator.MaxParametersForDeleteCommandsInPart = 10;

            var result = generator.GetDeleteCommands(GetFooList(25)).ToList();

            result.Should().HaveCount(3);
            result[0].CommandText.Should().Be(expectedQuery_0);
            result[1].CommandText.Should().Be(expectedQuery_1);
            result[2].CommandText.Should().Be(expectedQuery_2);

            GetParameterValues<int>(result[0].Parameters).Should().Equal(expectedParameters_0);
            GetParameterValues<int>(result[1].Parameters).Should().Equal(expectedParameters_1);
            GetParameterValues<int>(result[2].Parameters).Should().Equal(expectedParameters_2);
        }

        [Fact]
        public void FillCommandWithCorrectArguments()
        {
            var item = new Foo
            {
                Id = 336,
                Plat = 1500,
                KrstneMeno = "Homer",
                PropertyGuid = new Guid("{C0DC6F49-10A5-4AB7-9B9C-4152C25238BF}"),
                PropertyEnum = TestEnum.Value1,
                PropertyEnumConv = TestEnum.Value2
            };

            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Id, p.Plat, p.KrstneMeno, p.PropertyGuid, p.PropertyEnum, p.PropertyEnumConv });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
            DbCommand insert = generator.GetInsertCommand();
            generator.FillCommand(insert, item);

            insert.Parameters["@IdRow"].Value.Should().Be(336);
            insert.Parameters["@Salary"].Value.Should().Be((decimal)1500);
            insert.Parameters["@FirstName"].Value.Should().Be("Homer");
            insert.Parameters["@PropertyGuid"].Value.Should().Be(new Guid("{C0DC6F49-10A5-4AB7-9B9C-4152C25238BF}"));
            insert.Parameters["@PropertyEnum"].Value.Should().Be(1);
            insert.Parameters["@PropertyEnumConv"].Value.Should().Be("V2");
        }

        [Fact]
        public void ThrowMissingPrimaryKeyExceptionWhenGetUpdateCommand()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Plat, p.KrstneMeno, p.PropertyGuid, p.PropertyEnum, p.PropertyEnumConv });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false), provider, query);

            Action action = () =>
            {
                DbCommand update = generator.GetUpdateCommand();
            };
            action.Should().Throw<KORM.Exceptions.MissingPrimaryKeyException>();
        }

        [Fact]
        public void ThrowMissingPrimaryKeyExceptionWhenGetDeleteCommand()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Plat, p.KrstneMeno, p.PropertyGuid, p.PropertyEnum, p.PropertyEnumConv });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false), provider, query);
            Action action = () =>
            {
                DbCommand update = generator.GetDeleteCommand();
            };
            action.Should().Throw<KORM.Exceptions.MissingPrimaryKeyException>();
        }

        [Fact]
        public void UseConverter()
        {
            var idColumn = new ColumnInfo()
            {
                Name = nameof(ConverterDto.Id),
                PropertyInfo = GetPropertyInfo<ConverterDto>(nameof(ConverterDto.Id))
            };
            var nameColumn = new ColumnInfo()
            {
                Name = nameof(ConverterDto.Name),
                PropertyInfo = GetPropertyInfo<ConverterDto>(nameof(ConverterDto.Name)),
                Converter = new NullToStringConverter()
            };
            var tableInfo = new TableInfo(new[] { idColumn, nameColumn }, new List<PropertyInfo>(), null)
            {
                Name = nameof(ConverterDto)
            };

            KORM.Query.IQueryProvider queryProvider = Substitute.For<KORM.Query.IQueryProvider>();
            IDatabaseMapper mapper = Substitute.For<IDatabaseMapper>();
            mapper.GetTableInfo<ConverterDto>().Returns(tableInfo);
            var query = new Query<ConverterDto>(mapper, queryProvider);

            var generator = new CommandGenerator<ConverterDto>(tableInfo, queryProvider, query);
            var dto = new ConverterDto() { Id = 1, Name = null };
            var convertedValue = generator.GetColumnValue(nameColumn, dto);

            convertedValue.Should().Be("NULL");
        }

        #endregion

        #region Test Classes and Methods

        private List<T> GetParameterValues<T>(DbParameterCollection parameters)
        {
            var result = new List<T>();

            foreach (DbParameter prm in parameters)
            {
                result.Add((T)prm.Value);
            }

            return result;
        }

        private CommandGenerator<Foo> GetFooGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Id, p.Plat });
            return new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
        }

        private IQuery<Foo> CreateFooQuery()
        {
            var query = new Query<Foo>(
                new DatabaseMapper(new ConventionModelMapper()),
                new SqlServerQueryProvider(
                    new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(new DatabaseMapper(new ConventionModelMapper())),
                    Substitute.For<IModelBuilder>(),
                    new Logger()));

            return query;
        }

        private TableInfo GetFooTableInfo() => GetFooTableInfo(true);

        private TableInfo GetFooTableInfo(bool withIdRow)
        {
            var columns = new List<ColumnInfo>() {
                new ColumnInfo(){ Name = "FirstName", PropertyInfo = GetPropertyInfo<Foo>("KrstneMeno")},
                new ColumnInfo(){ Name = "Salary", PropertyInfo = GetPropertyInfo<Foo>("Plat")},
                new ColumnInfo(){ Name = "Birthday", PropertyInfo = GetPropertyInfo<Foo>("DatumNarodena")},
                new ColumnInfo(){ Name = "Is", PropertyInfo = GetPropertyInfo<Foo>("Is")},
                new ColumnInfo(){ Name = "PropertyGuid", PropertyInfo = GetPropertyInfo<Foo>("PropertyGuid")},
                new ColumnInfo(){ Name = "PropertyStringGuid", PropertyInfo = GetPropertyInfo<Foo>("PropertyStringGuid")},
                new ColumnInfo(){ Name = "PropertyEnum",  PropertyInfo = GetPropertyInfo<Foo>("PropertyEnum")},
                new ColumnInfo(){ Name = "PropertyDateTimeNullable", PropertyInfo = GetPropertyInfo<Foo>("PropertyDateTimeNullable")},
                new ColumnInfo(){ Name = "PropertyEnumConv", PropertyInfo = GetPropertyInfo<Foo>("PropertyEnumConv"), Converter = new TestEnumConverter()}
            };

            if (withIdRow)
            {
                columns.Add(new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<Foo>("Id"), IsPrimaryKey = true });
            }

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "Foo" };
        }

        private CommandGenerator<FooIdentity> GetFooIdentityGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<FooIdentity> query = CreateFooIdentityQuery();
            query.Select(p => new { p.Id, p.Plat });
            return new CommandGenerator<FooIdentity>(GetFooIdentityTableInfo(), provider, query);
        }

        private IQuery<FooIdentity> CreateFooIdentityQuery()
        {
            var query = new Query<FooIdentity>(
                new DatabaseMapper(new ConventionModelMapper()),
                new SqlServerQueryProvider(
                    new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(new DatabaseMapper(new ConventionModelMapper())),
                    Substitute.For<IModelBuilder>(),
                    new Logger()));

            return query;
        }

        private TableInfo GetFooIdentityTableInfo()
        {
            var columns = new List<ColumnInfo>() {
                new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<Foo>("Id"),
                    IsPrimaryKey = true, AutoIncrementMethodType = AutoIncrementMethodType.Identity },
                new ColumnInfo() { Name = "Salary", PropertyInfo = GetPropertyInfo<Foo>("Plat")}
            };

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "FooIdentity" };
        }

        private List<Foo> GetFooList(int itemsCount)
        {
            var retVal = new List<Foo>();

            for (int i = 0; i < itemsCount; i++)
            {
                retVal.Add(new Foo() { Id = (i + 1) });
            }

            return retVal;
        }

        private PropertyInfo GetPropertyInfo<T>(string propertyName) => typeof(T).GetProperty(propertyName);

        private class ConverterDto
        {
            public int Id { get; set; }

            [Converter(typeof(NullToStringConverter))]
            public string Name { get; set; }
        }

        private class NullToStringConverter : IConverter
        {
            public object Convert(object value) => value;
            public object ConvertBack(object value) => value is null ? "NULL" : value;
        }

        private class Foo
        {
            [Alias("IdRow")]
            [Key()]
            public int Id { get; set; }

            [Alias("FirstName")]
            public string KrstneMeno { get; set; }

            [Alias("Salary")]
            public decimal Plat { get; set; }

            [Alias("Birthday")]
            public DateTime DatumNarodenia { get; set; }

            public bool Is { get; set; }

            public Guid PropertyGuid { get; set; }

            [NoMap]
            public int Bar { get; set; }

            public TestEnum PropertyEnum { get; set; }

            public DateTime? PropertyDateTimeNullable { get; set; }

            [Converter(typeof(TestEnumConverter))]
            public TestEnum PropertyEnumConv { get; set; }
        }

        private class FooIdentity
        {
            [Alias("IdRow")]
            [Key(AutoIncrementMethodType.Identity)]
            public int Id { get; set; }

            [Alias("Salary")]
            public decimal Plat { get; set; }
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
                if ((TestEnum)value == TestEnum.Value1)
                {
                    return "V1";
                }
                else if ((TestEnum)value == TestEnum.Value2)
                {
                    return "V2";
                }
                else
                {
                    return "V3";
                }
            }
        }

        #endregion
    }
}
