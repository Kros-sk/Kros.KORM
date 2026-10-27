using Kros.Data.BulkActions;
using Kros.Data.Schema;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Query;
using Kros.KORM.Query.Providers;
using Kros.KORM.Query.Sql;
using Kros.KORM.UnitTests.Base;
using Kros.UnitTests;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Providers
{
    public class QueryProviderShould(KormTestsFixture kormContext) : DatabaseTestBase(kormContext)
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

#pragma warning disable IDE0060 // Remove unused parameter
            private TestQueryProvider(DbConnection internalConnection, bool isInternalConnection)
#pragma warning restore IDE0060 // Remove unused parameter
                : base(
                      new KormConnectionSettings() { ConnectionString = "QueryProviderTestConnectionString", KormProvider = "QueryProviderTest" },
                      Substitute.For<ISqlExpressionVisitorFactory>(),
                      new ModelBuilder(Database.DefaultModelFactory),
                      Substitute.For<ILogger>(),
                      Substitute.For<IDatabaseMapper>())
            {
                _dbProviderFactory = new TestDbProviderFactory(internalConnection);
            }

            private TestQueryProvider(DbConnection externalConnection)
                : base(externalConnection,
                      Substitute.For<ISqlExpressionVisitorFactory>(),
                      new ModelBuilder(Database.DefaultModelFactory),
                      Substitute.For<ILogger>(),
                      Substitute.For<IDatabaseMapper>())
            {
                _dbProviderFactory = new TestDbProviderFactory(null);
            }

            DbProviderFactory _dbProviderFactory;

            public override DbProviderFactory DbProviderFactory => _dbProviderFactory;

            public void CreateConnection()
            {
                var connection = Connection;
            }

            public override IBulkInsert CreateBulkInsert(object options) => throw new NotImplementedException();

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

        private class TestItemWithAmbiguousConstructors
        {
            public TestItemWithAmbiguousConstructors(int id)
            {
                Id = id;
            }

            public TestItemWithAmbiguousConstructors(int id, int number)
            {
                Id = id;
                Number = number;
            }

            public int Id { get; set; }
            public int Number { get; set; }
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
                var ex = Assert.Throws<ArgumentException>(executeNonQuery);
                Assert.Contains("@Param2", ex.Message);
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
                Assert.Equal(1, result); // Inserted 1 row.
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
                Assert.Equal(2, result); // Updated 2 rows.
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
                Assert.Equal(3, result); // Deleted 3 rows.
            }
        }

        [Fact]
        public async Task ExecuteNonQueryCommandParamsAsync()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"INSERT INTO {Table_TestTable} (Id, Number, Description) VALUES (@Id, @Number, @Description)";
                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = await provider.ExecuteNonQueryAsync(
                    query: query,
                    cancellationToken: TestContext.Current.CancellationToken,
                    paramValues: new object[] { 6, 666, "Sed ac lobortis magna." });
                Assert.Equal(1, result); // Inserted 1 row.
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
                int result = await provider.ExecuteNonQueryAsync(query: query,
                    cancellationToken: TestContext.Current.CancellationToken, parameters: parameters);
                Assert.Equal(1, result); // Inserted 1 row.
            }
        }

        [Fact]
        public async Task ExecuteNonQueryCommandWithoutParametersAsync()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                var query = $"DELETE FROM {Table_TestTable}";

                QueryProvider provider = CreateQueryProvider(helper.Connection);
                int result = await provider.ExecuteNonQueryAsync(query, TestContext.Current.CancellationToken);
                Assert.Equal(3, result); // Deleted 3 rows.
            }
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_ThrowsWhenTokenAlreadyCancelled_AndLeavesConnectionClosed()
        {
            using (var database = CreateDatabase(CreateTable_TestTable) as TestDatabase)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => database.ExecuteNonQueryAsync("SELECT 1", new CancellationToken(canceled: true)));

                Assert.Equal(ConnectionState.Closed, database.Connection.State);
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
                var ex = Assert.Throws<ArgumentException>(executeStoredProcedure);
                Assert.Contains("@Param2", ex.Message);
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

                Assert.Equal(expected, result);
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

                Assert.Equal(expected, result);
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

                Assert.Equal(inputParamValue * 2, parameters["@OutputParam"].Value);
                Assert.Equal(inputOutputParamValue * 2, parameters["@InputOutputParam"].Value);
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

                Assert.Equal(new TestItem(1, 10, "Lorem ipsum"), result);
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

                Assert.Equivalent(new TestItem[] {
                    new TestItem(1, 10, "Lorem ipsum"),
                    new TestItem(2, 20, null),
                    new TestItem(3, 30, "Hello world"),
                }, result);
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

                Assert.Equivalent(new TestItem[] {
                    new TestItem(1, 10, "Lorem ipsum"),
                    new TestItem(2, 20, null),
                    new TestItem(3, 30, "Hello world"),
                }, result);
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

                Assert.True(IsAnyReaderOpened(helper.Connection, Table_TestTable));
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

        [Fact]
        public async Task ExecuteAsyncReturnsAllRows()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Id], [Number], [Description] FROM {Table_TestTable}");

                List<TestItem> result = await ToListAsync(provider.ExecuteAsync(query, TestContext.Current.CancellationToken));

                Assert.Equivalent(new TestItem[] {
                    new TestItem(1, 10, "Lorem ipsum"),
                    new TestItem(2, 20, null),
                    new TestItem(3, 30, "Hello world"),
                }, result);
            }
        }

        [Fact]
        public async Task ExecuteAsyncExecutesQueryOnEachEnumeration()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Id], [Number], [Description] FROM {Table_TestTable}");
                IAsyncEnumerable<TestItem> items = provider.ExecuteAsync(query, TestContext.Current.CancellationToken);

                List<TestItem> first = await ToListAsync(items);
                await provider.ExecuteNonQueryAsync(
                    $"INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (4, 40, 'Added')",
                    TestContext.Current.CancellationToken);
                List<TestItem> second = await ToListAsync(items);

                Assert.Equal(3, first.Count);
                Assert.Equal(4, second.Count);
                Assert.Contains(new TestItem(4, 40, "Added"), second);
            }
        }

        [Fact]
        public async Task ExecuteAsyncThrowsOperationCanceledBeforeAnyRowIsRead()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Id], [Number], [Description] FROM {Table_TestTable}");
                var readItems = new List<TestItem>();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await foreach (TestItem item in provider.ExecuteAsync(query, new CancellationToken(canceled: true)))
                    {
                        readItems.Add(item);
                    }
                });

                Assert.Empty(readItems);
                Assert.True(IsAnyReaderOpened(helper.Connection, Table_TestTable));
            }
        }

        [Fact]
        public async Task ExecuteAsyncClosesReaderAndConnectionWhenEnumerationIsDisposedEarly()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            using (var connection = new SqlConnection(helper.Connection.ConnectionString))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Id], [Number], [Description] FROM {Table_TestTable}");
                Assert.Equal(ConnectionState.Closed, connection.State);

                IAsyncEnumerator<TestItem> enumerator = provider
                    .ExecuteAsync(query, TestContext.Current.CancellationToken)
                    .GetAsyncEnumerator(TestContext.Current.CancellationToken);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(ConnectionState.Open, connection.State);

                await enumerator.DisposeAsync();

                Assert.Equal(ConnectionState.Closed, connection.State);
                Assert.True(IsAnyReaderOpened(helper.Connection, Table_TestTable));
            }
        }

        [Fact]
        public async Task ExecuteAsyncClosesConnectionWhenExecuteReaderFails()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            using (var connection = new SqlConnection(helper.Connection.ConnectionString))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, "SELECT * FROM NoSuchTable");

                await Assert.ThrowsAsync<SqlException>(
                    async () => await ToListAsync(provider.ExecuteAsync(query, TestContext.Current.CancellationToken)));

                Assert.Equal(ConnectionState.Closed, connection.State);
            }
        }

        [Fact]
        public async Task ExecuteAsyncClosesConnectionWhenModelFactoryFails()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            using (var connection = new SqlConnection(helper.Connection.ConnectionString))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(connection);
                var query = new Query<TestItemWithAmbiguousConstructors>(mapper, provider);
                query.Sql("SELECT [Id], [Number], [Description] FROM " + Table_TestTable);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await ToListAsync(provider.ExecuteAsync(query, TestContext.Current.CancellationToken)));

                Assert.Equal(ConnectionState.Closed, connection.State);
                Assert.True(IsAnyReaderOpened(helper.Connection, Table_TestTable));
            }
        }

        [Fact]
        public void ExecuteAsyncThrowsArgumentNullExceptionEagerly()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, _) = CreateQueryProviderWithSqlGenerator(helper.Connection);

                Assert.Throws<ArgumentNullException>(
                    () => provider.ExecuteAsync<TestItem>(null, TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public async Task ExecuteScalarAsyncReturnsFirstColumnOfFirstRow()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Number], [Id] FROM {Table_TestTable} WHERE [Id] = 2");

                object result = await provider.ExecuteScalarAsync(query, TestContext.Current.CancellationToken);

                Assert.Equal(20, result);
            }
        }

        [Fact]
        public async Task ExecuteScalarAsyncReturnsNullForEmptyResult()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Number] FROM {Table_TestTable} WHERE [Id] = -1");

                object result = await provider.ExecuteScalarAsync(query, TestContext.Current.CancellationToken);

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task ExecuteScalarAsyncThrowsWhenTokenIsCancelled()
        {
            using (SqlServerTestHelper helper = CreateHelper(CreateTable_TestTable))
            {
                (SqlServerQueryProvider provider, IDatabaseMapper mapper) = CreateQueryProviderWithSqlGenerator(helper.Connection);
                IQuery<TestItem> query = CreateTestItemQuery(mapper, provider, $"SELECT [Number] FROM {Table_TestTable} WHERE [Id] = 2");

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => provider.ExecuteScalarAsync(query, new CancellationToken(canceled: true)));
            }
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
                Substitute.For<ILogger>(),
                Substitute.For<IDatabaseMapper>());

        private static (SqlServerQueryProvider Provider, IDatabaseMapper Mapper) CreateQueryProviderWithSqlGenerator(
            SqlConnection connection)
        {
            var mapper = new DatabaseMapper(new ConventionModelMapper());
            var provider = new SqlServerQueryProvider(
                connection,
                new SqlServerSqlExpressionVisitorFactory(mapper),
                new ModelBuilder(Database.DefaultModelFactory),
                Substitute.For<ILogger>(),
                mapper);
            return (provider, mapper);
        }

        private static IQuery<TestItem> CreateTestItemQuery(IDatabaseMapper mapper, QueryProvider provider, string sql)
        {
            var query = new Query<TestItem>(mapper, provider);
            query.Sql(sql);
            return query;
        }

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
        {
            var result = new List<T>();
            await foreach (T item in items)
            {
                result.Add(item);
            }
            return result;
        }

        private static SqlServerQueryProvider CreateQueryProvider(string connectionString)
            => new SqlServerQueryProvider(
                new KormConnectionSettings() { ConnectionString = connectionString },
                Substitute.For<ISqlExpressionVisitorFactory>(),
                new ModelBuilder(Database.DefaultModelFactory),
                Substitute.For<ILogger>(),
                Substitute.For<IDatabaseMapper>());

        #endregion
    }
}
