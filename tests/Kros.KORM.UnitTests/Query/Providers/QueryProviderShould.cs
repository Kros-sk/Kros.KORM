using FluentAssertions;
using Kros.Data.BulkActions;
using Kros.Data.Schema;
using Kros.Data.SqlServer;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Query;
using Kros.KORM.Query.Sql;
using Kros.KORM.UnitTests.Base;
using Kros.UnitTests;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class QueryProviderShould : DatabaseTestBase
    {
        #region Nested classes

        private class TestDbProviderFactory : DbProviderFactory
        {
            private readonly DbConnection _connection;

            public TestDbProviderFactory(DbConnection connection)
            {
                _connection = connection;
            }

            public override DbConnection CreateConnection()
            {
                return _connection ?? Substitute.For<DbConnection>();
            }
        }

        public class TestQueryProvider : QueryProvider
        {
            public static TestQueryProvider CreateWithInternalConnection(DbConnection connection)
            {
                return new TestQueryProvider(connection, true);
            }

            public static TestQueryProvider CreateWithExternalConnection(DbConnection connection)
            {
                return new TestQueryProvider(connection);
            }

            private TestQueryProvider(DbConnection internalConnection, bool isInternalConnection)
                : base(
                      new ConnectionStringSettings("QueryProviderTest", "QueryProviderTestConnectionString", "QueryProviderTest"),
                      Substitute.For<ISqlExpressionVisitorFactory>(),
                      new ModelBuilder(Database.DefaultModelFactory),
                      Substitute.For<ILogger>())
            {
                _dbProviderFactory = new TestDbProviderFactory(internalConnection);
            }

            private TestQueryProvider(DbConnection externalConnection)
                : base(externalConnection,
                      Substitute.For<ISqlExpressionVisitorFactory>(),
                      new ModelBuilder(Database.DefaultModelFactory),
                      Substitute.For<ILogger>())
            {
                _dbProviderFactory = new TestDbProviderFactory(null);
            }

            DbProviderFactory _dbProviderFactory;

            public override DbProviderFactory DbProviderFactory => _dbProviderFactory;

            public void CreateConnection()
            {
                var connection = Connection;
            }

            public override IBulkInsert CreateBulkInsert()
            {
                throw new NotImplementedException();
            }

            public override IBulkUpdate CreateBulkUpdate() => throw new NotImplementedException();

            protected override IDatabaseSchemaLoader GetSchemaLoader()
            {
                throw new NotImplementedException();
            }
        }

        private class TestItem
        {
            public TestItem()
            {
            }

            public TestItem(int id, int number, string description)
            {
                Id = id;
                Number = number;
                Description = description;
            }

            public int Id { get; set; }
            public int Number { get; set; }
            public string Description { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is TestItem item)
                {
                    return (item.Id == Id) && (item.Number == Number) && (item.Description == Description);
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion

        #region SQL Scripts

        private const string Table_TestTable = "TestTable";
        private const string Procedure_ScalarResult = "ScalarResult";
        private const string Procedure_OutputParameter = "OutputParameter";
        private const string Procedure_RowResultWithOneValue = "RowResultWithOneValue";
        private const string Procedure_RowResultWithMultipleValues = "RowResultWithMultipleValues";
        private const string Procedure_TableResult = "TableResult";

        private static string CreateTable_TestTable =
$@"CREATE TABLE[dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Number] [int] NOT NULL,
    [Description] [nvarchar] (50) NULL
) ON[PRIMARY];

INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (1, 10, 'Lorem ipsum');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (2, 20, NULL);
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (3, 30, 'Hello world');
";

        private static string CreateProcedure_TableResult =
$@"CREATE PROCEDURE [dbo].[{Procedure_TableResult}]
    @Param1 int = 0,
    @Param2 varchar(255) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Id], [Number], [Description] FROM {Table_TestTable}
END";

        private static string CreateProcedure_ScalarResult =
$@"CREATE PROCEDURE [dbo].[{Procedure_ScalarResult}]
    @Param1 int = 0,
    @Param2 int = 0
AS
BEGIN
    SET NOCOUNT ON;

    RETURN @Param1 + @Param2
END";

        private static string CreateProcedure_OutputParameter =
$@"CREATE PROCEDURE [dbo].[{Procedure_OutputParameter}]
    @InputParam int = 0,
    @InputOutputParam int = 0 OUTPUT,
    @OutputParam int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @InputOutputParam = @InputOutputParam * 2;
    SELECT @OutputParam = @InputParam * 2;
END";

        private static string CreateProcedure_RowResultWithOneValue =
$@"CREATE PROCEDURE [dbo].[{Procedure_RowResultWithOneValue}]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DATEFROMPARTS(1978, 12, 10)
END";

        private static string CreateProcedure_RowResultWithMultipleValues =
$@"CREATE PROCEDURE [dbo].[{Procedure_RowResultWithMultipleValues}]
    @Id int = 0,
    @Number int = 0,
    @Description varchar(255) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Id AS Id, @Number AS Number, @Description AS Description
END";

        #endregion

        #region Tests

        [Fact]
        public void ThrowExceptionWhenParameterIsNullAndDataTypeIsNotSetInQuery()
        {
            var parameters = new CommandParameterCollection
            {
                { "@Param1", 1 },
                { "@Param2", null }
            };

            using (SqlServerTestHelper helper = CreateHelper((string)null))
            {
                QueryProvider provider = CreateQueryProvider(helper.Connection);
                Action executeNonQuery = () => provider.ExecuteNonQuery("NO QUERY", parameters);
                executeNonQuery.Should().Throw<ArgumentException>().WithMessage("*@Param2*");
            }
        }

        [Fact]
        public void ExecuteInsertQuery()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"INSERT INTO {Table_TestTable} (Id, Number, Description) VALUES (@Id, @Number, @Description)";
                var parameters = new CommandParameterCollection
                {
                    { "@Id", 6 },
                    { "@Number", 666 },
                    { "@Description", "Sed ac lobortis magna." }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = provider.ExecuteNonQuery(query, parameters);
                result.Should().Be(1); // Inserted 1 row.
            }
        }

        [Fact]
        public void ExecuteUpdateQuery()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"UPDATE {Table_TestTable} SET Number = @Number WHERE Id >= @Id";
                var parameters = new CommandParameterCollection
                {
                    { "@Id", 2 },
                    { "@Number", 666 }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = provider.ExecuteNonQuery(query, parameters);
                result.Should().Be(2); // Updated 2 rows.
            }
        }

        [Fact]
        public void ExecuteDeleteQuery()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"DELETE FROM {Table_TestTable}";

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = provider.ExecuteNonQuery(query);
                result.Should().Be(3); // Deleted 3 rows.
            }
        }

        [Fact]
        public async Task ExecuteNonQueryCommandAsync()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"INSERT INTO {Table_TestTable} (Id, Number, Description) VALUES (@Id, @Number, @Description)";
                var parameters = new CommandParameterCollection
                {
                    { "@Id", 6 },
                    { "@Number", 666 },
                    { "@Description", "Sed ac lobortis magna." }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = await provider.ExecuteNonQueryAsync(query, parameters);
                result.Should().Be(1); // Inserted 1 row.
            }
        }

        [Fact]
        public async Task ExecuteNonQueryCommandWithoutParametersAsync()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"DELETE FROM {Table_TestTable}";

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = await provider.ExecuteNonQueryAsync(query);
                result.Should().Be(3); // Deleted 3 rows.
            }
        }

        [Fact]
        public void ThrowExceptionWhenParameterIsNullAndDataTypeIsNotSetInStoredProcedure()
        {
            var parameters = new CommandParameterCollection
            {
                { "@Param1", 1 },
                { "@Param2", null }
            };

            using (SqlServerTestHelper helper = CreateHelper((string)null))
            {
                QueryProvider provider = CreateQueryProvider(helper.Connection);
                Action executeStoredProcedure = () => provider.ExecuteStoredProcedure<int>("NoProcedure", parameters);
                executeStoredProcedure.Should().Throw<ArgumentException>().WithMessage("*@Param2*");
            }
        }

        [Fact]
        public void ReturnCorrectValueFromStoredProcedure()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateProcedure_ScalarResult))
            {
                const int param1 = 11;
                const int param2 = 22;
                const int expected = 33;

                var parameters = new CommandParameterCollection
                {
                    { "@Param1", param1 },
                    { "@Param2", param2 }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = provider.ExecuteStoredProcedure<int>(Procedure_ScalarResult, parameters);

                result.Should().Be(expected);
            }
        }

        [Fact]
        public void ReturnCorrectValueFromSelectScalarStoredProcedure()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateProcedure_RowResultWithOneValue))
            {
                DateTime expected = new DateTime(1978, 12, 10);

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                DateTime result = provider.ExecuteStoredProcedure<DateTime>(Procedure_RowResultWithOneValue);

                result.Should().Be(expected);
            }
        }

        [Fact]
        public void ReturnCorrectValueFromStoredProcedureUsingOutputParameter()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateProcedure_OutputParameter))
            {
                const int inputParamValue = 10;
                const int inputOutputParamValue = 100;

                var parameters = new CommandParameterCollection
                {
                    { "@InputParam", inputParamValue },
                    { "@InputOutputParam", inputOutputParamValue, DbType.Int32, ParameterDirection.InputOutput },
                    { "@OutputParam", 0, DbType.Int32, ParameterDirection.Output }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = provider.ExecuteStoredProcedure<int>(Procedure_OutputParameter, parameters);

                parameters["@OutputParam"].Value.Should().Be(inputParamValue * 2);
                parameters["@InputOutputParam"].Value.Should().Be(inputOutputParamValue * 2);
            }
        }

        [Fact]
        public void ReturnCorrectValueFromSelectRowStoredProcedure()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateProcedure_RowResultWithMultipleValues))
            {
                var parameters = new CommandParameterCollection
                {
                    { "@Id", 1 },
                    { "@Number", 10 },
                    { "@Description", "Lorem ipsum" }
                };

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                TestItem result = provider.ExecuteStoredProcedure<TestItem>(Procedure_RowResultWithMultipleValues, parameters);

                result.Should().Be(new TestItem(1, 10, "Lorem ipsum"));
            }
        }

        [Fact]
        public void ReturnCorrectItemsFromSelectTableStoredProcedure()
        {
            string[] initScripts = { CreateTable_TestTable, CreateProcedure_TableResult };
            using (SqlServerTestHelper helper = CreateHelper(initScripts))
            {
                QueryProvider provider = CreateQueryProvider(helper.Connection);
                List<TestItem> result = provider.ExecuteStoredProcedure<IEnumerable<TestItem>>(Procedure_TableResult).ToList();

                result.Should().BeEquivalentTo(new TestItem[] {
                    new TestItem(1, 10, "Lorem ipsum"),
                    new TestItem(2, 20, null),
                    new TestItem(3, 30, "Hello world"),
                });
            }
        }

        [Fact]
        public void ReturnCorrectItemsFromSelectTableStoredProcedure_WhenUseConnectionString()
        {
            string[] initScripts = { CreateTable_TestTable, CreateProcedure_TableResult };
            using (SqlServerTestHelper helper = CreateHelper(initScripts))
            {
                QueryProvider provider = CreateQueryProvider(helper.Connection.ConnectionString);
                List<TestItem> result = provider.ExecuteStoredProcedure<IEnumerable<TestItem>>(Procedure_TableResult).ToList();

                result.Should().BeEquivalentTo(new TestItem[] {
                    new TestItem(1, 10, "Lorem ipsum"),
                    new TestItem(2, 20, null),
                    new TestItem(3, 30, "Hello world"),
                });
            }
        }

        [Fact]
        public void CloseReaderWhenCallStoredProcedure()
        {
            string[] initScripts = { CreateTable_TestTable, CreateProcedure_TableResult };
            using (SqlServerTestHelper helper = CreateHelper(initScripts))
            {
                QueryProvider provider = CreateQueryProvider(helper.Connection);
                List<TestItem> result = provider.ExecuteStoredProcedure<IEnumerable<TestItem>>(Procedure_TableResult).ToList();

                IsAnyReaderOpened(helper.Connection, Table_TestTable)
                    .Should().BeTrue();
            }
        }

        [Fact]
        public void DisposeOfInternalConnection()
        {
            DbConnection connection = Substitute.For<DbConnection>();
            using (TestQueryProvider provider = TestQueryProvider.CreateWithInternalConnection(connection))
            {
                provider.CreateConnection();
                provider.Dispose();
            }

            connection.Received().Dispose();
        }

        [Fact]
        public void NotDisposeOfExternalConnection()
        {
            DbConnection connection = Substitute.For<DbConnection>();
            using (TestQueryProvider provider = TestQueryProvider.CreateWithExternalConnection(connection))
            {
                provider.CreateConnection();
                provider.Dispose();
            }

            connection.DidNotReceive().Dispose();
        }

        #endregion

        #region Helpers

        private SqlServerTestHelper CreateHelper(string initScript)
        {
            return new SqlServerTestHelper(BaseConnectionString, BaseDatabaseName, initScript);
        }

        private SqlServerTestHelper CreateHelper(IEnumerable<string> initScripts)
        {
            return new SqlServerTestHelper(BaseConnectionString, BaseDatabaseName, initScripts);
        }

        private static SqlServerQueryProvider CreateQueryProvider(SqlConnection connection)
            => new SqlServerQueryProvider(
                connection,
                Substitute.For<ISqlExpressionVisitorFactory>(),
                new ModelBuilder(Database.DefaultModelFactory),
                Substitute.For<ILogger>());

        private static SqlServerQueryProvider CreateQueryProvider(string connectionString)
            => new SqlServerQueryProvider(
                new ConnectionStringSettings("Default", connectionString, SqlServerDataHelper.ClientId),
                Substitute.For<ISqlExpressionVisitorFactory>(),
                new ModelBuilder(Database.DefaultModelFactory),
                Substitute.For<ILogger>());

        #endregion
    }
}
