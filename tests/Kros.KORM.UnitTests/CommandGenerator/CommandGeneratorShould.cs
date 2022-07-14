using FluentAssertions;
using FluentAssertions.Common;
using Kros.KORM.CommandGenerator;
using Kros.KORM.Converter;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.Providers;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
            const string expectedQuery = @"DECLARE @OutputTable TABLE (IdRow int);
INSERT INTO [FooIdentity] ([Salary]) OUTPUT INSERTED.IdRow INTO @OutputTable VALUES (@Salary);
SELECT * FROM @OutputTable;";

            DbCommand insert = GetFooIdentityGenerator().GetInsertCommand();

            insert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectUpdateCommandText()
        {
            const string expectedQuery = "UPDATE [Foo] SET [Salary] = @Salary, [PropertyValueGenerator] = @PropertyValueGenerator WHERE ([IdRow] = @IdRow)";

            DbCommand update = GetFooGenerator().GetUpdateCommand();

            update.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectUpsertCommandText()
        {
            const string expectedQuery = "MERGE INTO [Foo] dst " +
                "USING(SELECT @IdRow IdRow) src " +
                "ON src.[IdRow] = dst.[IdRow] " +
                "WHEN MATCHED THEN UPDATE SET [Salary] = @Salary, [PropertyValueGenerator] = @PropertyValueGenerator " +
                "WHEN NOT MATCHED THEN INSERT([IdRow], [Salary]) VALUES (@IdRow, @Salary) ;";

            DbCommand upsert = GetFooGenerator().GetUpsertCommand();

            upsert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectUpsertCommandTextForPrimaryKeyOnly()
        {
            const string expectedQuery = "MERGE INTO [FooPrimaryKeys] dst " +
                "USING(SELECT @FK1 FK1, @FK2 FK2) src " +
                "ON src.[FK1] = dst.[FK1] AND src.[FK2] = dst.[FK2] " +
                "WHEN NOT MATCHED THEN INSERT([FK1], [FK2]) VALUES (@FK1, @FK2) ;";

            CommandGenerator<FooPrimaryKeys> commandGenerator = GetFooPrimaryKeyGenerator();

            DbCommand upsert = commandGenerator.GetUpsertCommand();

            upsert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void HaveCorrectUpsertCommandTextForCustomCondition()
        {
            const string expectedQuery = "MERGE INTO [Foo] dst " +
                "USING(SELECT @FirstName FirstName, @PropertyEnum PropertyEnum) src " +
                "ON src.[FirstName] = dst.[FirstName] AND src.[PropertyEnum] = dst.[PropertyEnum] " +
                "WHEN MATCHED THEN UPDATE SET [Salary] = @Salary, [PropertyValueGenerator] = @PropertyValueGenerator " +
                "WHEN NOT MATCHED THEN INSERT([IdRow], [FirstName], [PropertyEnum], [Salary]) VALUES (@IdRow, @FirstName, @PropertyEnum, @Salary) ;";

            DbCommand upsert = GetUpsertFooGenerator().GetUpsertCommand(new[] { "FirstName", "PropertyEnum" });

            upsert.CommandText.Should().Be(expectedQuery);
        }

        [Fact]
        public void ThrowArgumentExceptionOnMissingColumnForUpsertCommand()
        {
            var generator = GetUpsertFooGenerator();
            Action action = () =>
            {
                DbCommand update = generator.GetUpsertCommand(new[] { "FirstName", "MissingColumn" });
            };
            action.Should().Throw<InvalidOperationException>();
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

            var result = GetFooGenerator().GetDeleteCommands(Enumerable.Range(1, 15)).ToList();

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

            var result = generator.GetDeleteCommands(Enumerable.Range(1, 25)).ToList();

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
            generator.FillCommand(insert, item, ValueGenerated.OnInsert);

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

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false, false), provider, query);

            Action action = () =>
            {
                DbCommand update = generator.GetUpdateCommand();
            };
            action.Should().Throw<KORM.Exceptions.MissingPrimaryKeyException>();
        }

        [Fact]
        public void ThrowMissingPrimaryKeyExceptionWhenGetUpsertCommand()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Plat, p.KrstneMeno, p.PropertyGuid, p.PropertyEnum, p.PropertyEnumConv });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false, false), provider, query);

            Action action = () =>
            {
                DbCommand update = generator.GetUpsertCommand();
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

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false, false), provider, query);
            Action action = () =>
            {
                DbCommand update = generator.GetDeleteCommand();
            };
            action.Should().Throw<KORM.Exceptions.MissingPrimaryKeyException>();
        }

        [Fact]
        public void UseConverter()
        {
            TableInfo tableInfo = CreateTableInfoFromDto<ConverterDto>();
            tableInfo.Columns.Single(c => c.Name == nameof(ConverterDto.Name)).Converter = new NullToStringConverter();
            ColumnInfo nameColumn = tableInfo.Columns.Single(col => col.Name == nameof(ConverterDto.Name));

            CommandGenerator<ConverterDto> commandGenerator = CreateCommandGenerator<ConverterDto>(tableInfo);

            var dto = new ConverterDto() { Id = 1, Name = null };
            var convertedValue = commandGenerator.GetColumnValue(nameColumn, dto, ValueGenerated.Never);

            convertedValue.Should().Be("NULL");
        }

        [Fact]
        public void UseValueGenerator()
        {
            TableInfo tableInfo = CreateTableInfoFromDto<ConverterDto>();

            tableInfo.Columns.Single(c => c.Name == nameof(ConverterDto.Id)).ValueGenerator = new AutoIncrementValueGenerator();
            ColumnInfo idColumn = tableInfo.Columns.Single(col => col.Name == nameof(ConverterDto.Id));

            CommandGenerator<ConverterDto> commandGenerator = CreateCommandGenerator<ConverterDto>(tableInfo);

            var dto = new ConverterDto() { Id = 1, Name = null };
            commandGenerator.SetColumnValueFromValueGenerator(idColumn, dto, ValueGenerated.Never);
            var convertedValue = commandGenerator.GetColumnValue(idColumn, dto, ValueGenerated.Never);

            convertedValue.Should().Be(AutoIncrementValueGenerator.GeneratedValue);
        }

        [Fact]
        public void CommandShouldContainGeneratedValueWhenNotIgnored()
        {
            var item = new Foo
            {
                PropertyValueGenerator = 552
            };

            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.PropertyValueGenerator });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(true), provider, query);

            DbCommand insert = generator.GetInsertCommand();
            DbCommand update = generator.GetUpdateCommand();

            generator.FillCommand(insert, item, ValueGenerated.OnInsert);
            insert.Parameters["@PropertyValueGenerator"].Value.Should().Be(123);

            generator.FillCommand(update, item, ValueGenerated.OnUpdate);
            update.Parameters["@PropertyValueGenerator"].Value.Should().Be(123);
        }

        [Fact]
        public void CommandShouldNotContainGeneratedValueWhenIgnored()
        {
            var item = new Foo
            {
                PropertyValueGenerator = 552
            };

            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.PropertyValueGenerator });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(true), provider, query);

            DbCommand insert = generator.GetInsertCommand();
            DbCommand update = generator.GetUpdateCommand();

            generator.FillCommand(insert, item, ValueGenerated.OnInsert, true);
            insert.Parameters["@PropertyValueGenerator"].Value.Should().Be(552);

            generator.FillCommand(update, item, ValueGenerated.OnUpdate, true);
            update.Parameters["@PropertyValueGenerator"].Value.Should().Be(552);
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
            query.Select(p => new { p.Id, p.Plat, p.PropertyValueGenerator });
            return new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
        }

        private CommandGenerator<Foo> GetUpsertFooGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Id, p.KrstneMeno, p.PropertyEnum, p.Plat, p.PropertyValueGenerator });
            return new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
        }

        private IQuery<Foo> CreateFooQuery()
            => CreateQuery<Foo>();

        private IQuery<T> CreateQuery<T>()
        {
            var query = new Query<T>(
                new DatabaseMapper(new ConventionModelMapper()),
                new SqlServerQueryProvider(
                    new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(new DatabaseMapper(new ConventionModelMapper())),
                    Substitute.For<IModelBuilder>(),
                    new Logger(),
                    Substitute.For<IDatabaseMapper>()));

            return query;
        }

        private TableInfo GetFooTableInfo(bool isValueGeneratedOnInsertOrUpdate = false) => GetFooTableInfo(true, isValueGeneratedOnInsertOrUpdate);

        private TableInfo GetFooTableInfo(bool withIdRow, bool isValueGeneratedOnInsertOrUpdate)
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
                new ColumnInfo(){ Name = "PropertyEnumConv", PropertyInfo = GetPropertyInfo<Foo>("PropertyEnumConv"), Converter = new TestEnumConverter()},
                new ColumnInfo(){
                    Name = "PropertyValueGenerator",
                    PropertyInfo = GetPropertyInfo<Foo>("PropertyValueGenerator"),
                    ValueGenerator = new AutoIncrementValueGenerator(),
                    ValueGenerated = isValueGeneratedOnInsertOrUpdate ? ValueGenerated.OnInsertOrUpdate : ValueGenerated.OnUpdate
                }
            };

            if (withIdRow)
            {
                columns.Add(new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<Foo>("Id"), IsPrimaryKey = true });
            }

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "Foo" };
        }

        private CommandGenerator<FooPrimaryKeys> GetFooPrimaryKeyGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<FooPrimaryKeys> query = CreateQuery<FooPrimaryKeys>();
            query.Select(p => new { FK1 = 1, FK2 = 2 });
            TableInfo tableInfo = CreateTableInfoFromDto<FooPrimaryKeys>();
            return new CommandGenerator<FooPrimaryKeys>(tableInfo, provider, query);
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
                    new Logger(),
                    Substitute.For<IDatabaseMapper>()));

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

        private PropertyInfo GetPropertyInfo<T>(string propertyName) => typeof(T).GetProperty(propertyName);

        private TableInfo CreateTableInfoFromDto<T>()
        {
            var columns = new List<ColumnInfo>();
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                columns.Add(new ColumnInfo()
                {
                    Name = property.Name,
                    PropertyInfo = property,
                    IsPrimaryKey = property.IsDecoratedWith<KeyAttribute>()
                });
            }
            return new TableInfo(columns, new List<PropertyInfo>(), null)
            {
                Name = typeof(T).Name
            };
        }

        private CommandGenerator<T> CreateCommandGenerator<T>(TableInfo tableInfo)
        {
            IDatabaseMapper mapper = Substitute.For<IDatabaseMapper>();
            mapper.GetTableInfo<T>().Returns(tableInfo);
            KORM.Query.IQueryProvider queryProvider = Substitute.For<KORM.Query.IQueryProvider>();
            var query = new Query<T>(mapper, queryProvider);

            return new CommandGenerator<T>(tableInfo, queryProvider, query);
        }

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

            [Alias("PropertyValueGenerator")]
            public int PropertyValueGenerator { get; set; }
        }

        private class FooIdentity
        {
            [Alias("IdRow")]
            [Key(AutoIncrementMethodType.Identity)]
            public int Id { get; set; }

            [Alias("Salary")]
            public decimal Plat { get; set; }
        }

        private class FooPrimaryKeys
        {
            [Key(1)]
            public int FK1 { get; set; }

            [Key(2)]
            public int FK2 { get; set; }
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

        private class AutoIncrementValueGenerator : IValueGenerator
        {
            public const int GeneratedValue = 123;
            public object GetValue() => GeneratedValue;
        }

        #endregion
    }
}
