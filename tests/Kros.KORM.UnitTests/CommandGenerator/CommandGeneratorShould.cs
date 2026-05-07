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

            Assert.Equal(expectedQuery, insert.CommandText);
        }

        [Fact]
        public void HaveCorrectInsertCommandTextWhenTableHaveIdentityPrimaryKey()
        {
            const string expectedQuery = @"DECLARE @OutputTable TABLE (IdRow int);
INSERT INTO [FooIdentity] ([Salary]) OUTPUT INSERTED.IdRow INTO @OutputTable VALUES (@Salary);
SELECT * FROM @OutputTable;";

            DbCommand insert = GetFooIdentityGenerator().GetInsertCommand();

            Assert.Equal(expectedQuery, insert.CommandText);
        }

        [Fact]
        public void HaveCorrectInsertCommandTextWhenTableHasGuidIdentityPrimaryKey()
        {
            const string expectedQuery = @"DECLARE @OutputTable TABLE (IdRow uniqueidentifier);
INSERT INTO [FooGuidIdentity] ([Salary]) OUTPUT INSERTED.IdRow INTO @OutputTable VALUES (@Salary);
SELECT * FROM @OutputTable;";

            DbCommand insert = GetFooGuidIdentityGenerator().GetInsertCommand();

            Assert.Equal(expectedQuery, insert.CommandText);
        }

        [Fact]
        public void HaveCorrectUpdateCommandText()
        {
            const string expectedQuery = "UPDATE [Foo] SET [Salary] = @Salary, [PropertyValueGenerator] = @PropertyValueGenerator WHERE ([IdRow] = @IdRow)";

            DbCommand update = GetFooGenerator().GetUpdateCommand();

            Assert.Equal(expectedQuery, update.CommandText);
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

            Assert.Equal(expectedQuery, upsert.CommandText);
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

            Assert.Equal(expectedQuery, upsert.CommandText);
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

            Assert.Equal(expectedQuery, upsert.CommandText);
        }

        [Fact]
        public void ThrowArgumentExceptionOnMissingColumnForUpsertCommand()
        {
            var generator = GetUpsertFooGenerator();
            Action action = () =>
            {
                DbCommand update = generator.GetUpsertCommand(new[] { "FirstName", "MissingColumn" });
            };
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void HaveCorrectDeleteCommandText()
        {
            const string expectedQuery = "DELETE FROM [Foo] WHERE ([IdRow] = @IdRow)";

            DbCommand delete = GetFooGenerator().GetDeleteCommand();

            Assert.Equal(expectedQuery, delete.CommandText);
        }

        [Fact]
        public void HaveCorrectOneDeleteQuery()
        {
            const string expectedQuery = "DELETE FROM [Foo] WHERE [IdRow] IN (@P1,@P2,@P3,@P4,@P5,@P6,@P7,@P8,@P9,@P10,@P11,@P12,@P13,@P14,@P15)";

            var result = GetFooGenerator().GetDeleteCommands(Enumerable.Range(1, 15)).ToList();

            Assert.Single(result);
            Assert.Equal(expectedQuery, result[0].CommandText);
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

            Assert.Equal(3, result.Count);
            Assert.Equal(expectedQuery_0, result[0].CommandText);
            Assert.Equal(expectedQuery_1, result[1].CommandText);
            Assert.Equal(expectedQuery_2, result[2].CommandText);

            Assert.Equal(expectedParameters_0, GetParameterValues<int>(result[0].Parameters));
            Assert.Equal(expectedParameters_1, GetParameterValues<int>(result[1].Parameters));
            Assert.Equal(expectedParameters_2, GetParameterValues<int>(result[2].Parameters));
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

            Assert.Equal(336, insert.Parameters["@IdRow"].Value);
            Assert.Equal((decimal)1500, insert.Parameters["@Salary"].Value);
            Assert.Equal("Homer", insert.Parameters["@FirstName"].Value);
            Assert.Equal(new Guid("{C0DC6F49-10A5-4AB7-9B9C-4152C25238BF}"), insert.Parameters["@PropertyGuid"].Value);
            Assert.Equal(1, insert.Parameters["@PropertyEnum"].Value);
            Assert.Equal("V2", insert.Parameters["@PropertyEnumConv"].Value);
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
            Assert.Throws<KORM.Exceptions.MissingPrimaryKeyException>(action);
        }

        [Fact]
        public void ThrowMissingPrimaryKeyExceptionWhenGetUpsertCommand()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(new SqlCommand());

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Plat, p.KrstneMeno, p.PropertyGuid, p.PropertyEnum, p.PropertyEnumConv });

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(false), provider, query);

            Action action = () =>
            {
                DbCommand update = generator.GetUpsertCommand();
            };
            Assert.Throws<KORM.Exceptions.MissingPrimaryKeyException>(action);
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
            Assert.Throws<KORM.Exceptions.MissingPrimaryKeyException>(action);
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

            Assert.Equal("NULL", convertedValue);
        }

        [Fact]
        public void UseValueGenerator()
        {
            TableInfo tableInfo = CreateTableInfoFromDto<ConverterDto>();

            tableInfo.Columns.Single(c => c.Name == nameof(ConverterDto.Id)).ValueGenerator = new AutoIncrementValueGenerator();
            ColumnInfo idColumn = tableInfo.Columns.Single(col => col.Name == nameof(ConverterDto.Id));

            CommandGenerator<ConverterDto> commandGenerator = CreateCommandGenerator<ConverterDto>(tableInfo);

            var dto = new ConverterDto() { Id = 1, Name = null };
            CommandGenerator<ConverterDto>.SetColumnValueFromValueGenerator(idColumn, dto, ValueGenerated.Never);
            var convertedValue = commandGenerator.GetColumnValue(idColumn, dto, ValueGenerated.Never);

            Assert.Equal(AutoIncrementValueGenerator.GeneratedValue, convertedValue);
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

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(ValueGenerated.OnInsertOrUpdate), provider, query);

            DbCommand insert = generator.GetInsertCommand();
            DbCommand update = generator.GetUpdateCommand();

            generator.FillCommand(insert, item, ValueGenerated.OnInsert);
            Assert.Equal(123, insert.Parameters["@PropertyValueGenerator"].Value);

            generator.FillCommand(update, item, ValueGenerated.OnUpdate);
            Assert.Equal(123, update.Parameters["@PropertyValueGenerator"].Value);
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

            var generator = new CommandGenerator<Foo>(GetFooTableInfo(ValueGenerated.OnInsertOrUpdate), provider, query);

            DbCommand insert = generator.GetInsertCommand();
            DbCommand update = generator.GetUpdateCommand();

            generator.FillCommand(insert, item, ValueGenerated.OnInsert, true);
            Assert.Equal(552, insert.Parameters["@PropertyValueGenerator"].Value);

            generator.FillCommand(update, item, ValueGenerated.OnUpdate, true);
            Assert.Equal(552, update.Parameters["@PropertyValueGenerator"].Value);
        }

        #endregion

        #region Test Classes and Methods

        private static List<T> GetParameterValues<T>(DbParameterCollection parameters)
        {
            var result = new List<T>();

            foreach (DbParameter prm in parameters)
            {
                result.Add((T)prm.Value);
            }

            return result;
        }

        private static CommandGenerator<Foo> GetFooGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Id, p.Plat, p.PropertyValueGenerator });
            return new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
        }

        private static CommandGenerator<Foo> GetUpsertFooGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<Foo> query = CreateFooQuery();
            query.Select(p => new { p.Id, p.KrstneMeno, p.PropertyEnum, p.Plat, p.PropertyValueGenerator });
            return new CommandGenerator<Foo>(GetFooTableInfo(), provider, query);
        }

        private static CommandGenerator<FooIdentity> GetFooIdentityGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<FooIdentity> query = CreateFooIdentityQuery();
            query.Select(p => new { p.Id, p.Plat });
            return new CommandGenerator<FooIdentity>(GetFooIdentityTableInfo(), provider, query);
        }

        private static CommandGenerator<FooGuidIdentity> GetFooGuidIdentityGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<FooGuidIdentity> query = CreateFooGuidIdentityQuery();
            query.Select(p => new { p.Id, p.Plat });
            return new CommandGenerator<FooGuidIdentity>(GetFooGuidIdentityTableInfo(), provider, query);
        }

        private static Query<Foo> CreateFooQuery()
            => CreateQuery<Foo>();

        private static Query<FooIdentity> CreateFooIdentityQuery()
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

        private static Query<FooGuidIdentity> CreateFooGuidIdentityQuery()
        {
            var query = new Query<FooGuidIdentity>(
                new DatabaseMapper(new ConventionModelMapper()),
                new SqlServerQueryProvider(
                    new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(new DatabaseMapper(new ConventionModelMapper())),
                    Substitute.For<IModelBuilder>(),
                    new Logger(),
                    Substitute.For<IDatabaseMapper>()));

            return query;
        }

        private static Query<T> CreateQuery<T>()
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

        private static TableInfo GetFooTableInfo(
            ValueGenerated valueGenerated = ValueGenerated.OnUpdate)
            => GetFooTableInfo(true, valueGenerated);

        private static TableInfo GetFooTableInfo(
            bool withIdRow,
            ValueGenerated valueGenerated = ValueGenerated.OnUpdate)
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
                    ValueGenerated = valueGenerated
                }
            };

            if (withIdRow)
            {
                columns.Add(new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<Foo>("Id"), IsPrimaryKey = true });
            }

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "Foo" };
        }

        private static TableInfo GetFooIdentityTableInfo()
        {
            var columns = new List<ColumnInfo>() {
                new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<Foo>("Id"),
                    IsPrimaryKey = true, AutoIncrementMethodType = AutoIncrementMethodType.Identity },
                new ColumnInfo() { Name = "Salary", PropertyInfo = GetPropertyInfo<Foo>("Plat")}
            };

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "FooIdentity" };
        }

        private static TableInfo GetFooGuidIdentityTableInfo()
        {
            var columns = new List<ColumnInfo>() {
                new ColumnInfo() { Name = "IdRow", PropertyInfo = GetPropertyInfo<FooGuidIdentity>("Id"),
                    IsPrimaryKey = true, AutoIncrementMethodType = AutoIncrementMethodType.Identity },
                new ColumnInfo() { Name = "Salary", PropertyInfo = GetPropertyInfo<FooGuidIdentity>("Plat")}
            };

            return new TableInfo(columns, new List<PropertyInfo>(), null) { Name = "FooGuidIdentity" };
        }

        private static CommandGenerator<FooPrimaryKeys> GetFooPrimaryKeyGenerator()
        {
            KORM.Query.IQueryProvider provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.GetCommandForCurrentTransaction().Returns(a => { return new SqlCommand(); });

            IQuery<FooPrimaryKeys> query = CreateQuery<FooPrimaryKeys>();
            query.Select(p => new { FK1 = 1, FK2 = 2 });
            TableInfo tableInfo = CreateTableInfoFromDto<FooPrimaryKeys>();
            return new CommandGenerator<FooPrimaryKeys>(tableInfo, provider, query);
        }

        private static PropertyInfo GetPropertyInfo<T>(string propertyName) => typeof(T).GetProperty(propertyName);

        private static TableInfo CreateTableInfoFromDto<T>()
        {
            var columns = new List<ColumnInfo>();
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                columns.Add(new ColumnInfo()
                {
                    Name = property.Name,
                    PropertyInfo = property,
                    IsPrimaryKey = property.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute))
                });
            }
            return new TableInfo(columns, new List<PropertyInfo>(), null)
            {
                Name = typeof(T).Name
            };
        }

        private static CommandGenerator<T> CreateCommandGenerator<T>(TableInfo tableInfo)
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

        private class FooGuidIdentity
        {
            [Alias("IdRow")]
            [Key(AutoIncrementMethodType.Identity)]
            public Guid Id { get; set; }

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
