using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class AsyncQueryTests(KormTestsFixture kormContext) : DatabaseTestBase(kormContext)
    {
        #region Nested Classes

        private class Foo
        {
            public int Id { get; set; }

            public int UserId { get; set; }

            public string Value { get; set; }
        }

        #endregion

        #region SQL Scripts

        private const string Table_Foo = "Foo";

        private static readonly string CreateTable_FooTable =
$@"CREATE TABLE [dbo].[Foo] (
    [Id] [int] NOT NULL,
    [UserId] [int],
    [Value] [nvarchar](255)
) ON [PRIMARY];";

        private static readonly string InsertIntoFooScript =
$@"INSERT INTO [Foo] VALUES (1, 1, 'Milan');
INSERT INTO [Foo] VALUES (2, 1, 'Peter');
INSERT INTO [Foo] VALUES (3, 2, 'Juraj');
INSERT INTO [Foo] VALUES (4, 3, 'Jakub');";

        #endregion

        [Fact]
        public async Task ToListAsyncReturnsAllRows()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            List<Foo> items = await database.Query<Foo>().ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(4, items.Count);
            Assert.Equivalent(new[] { 1, 2, 3, 4 }, items.Select(p => p.Id));
        }

        [Fact]
        public async Task FirstOrDefaultAsyncWithConditionReturnsMatchingRow()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Foo foo = await database.Query<Foo>()
                .FirstOrDefaultAsync("Id = @1", TestContext.Current.CancellationToken, 3);

            Assert.NotNull(foo);
            Assert.Equal("Juraj", foo.Value);
        }

        [Fact]
        public async Task FirstOrDefaultAsyncReturnsNullWhenNoRowMatches()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);
            int id = 42;

            Foo foo = await database.Query<Foo>()
                .FirstOrDefaultAsync($"Id = {id}", TestContext.Current.CancellationToken);

            Assert.Null(foo);
        }

        [Fact]
        public async Task AnyAsyncReturnsTrueWhenRowsExist()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Assert.True(await database.Query<Foo>().AnyAsync(TestContext.Current.CancellationToken));
            Assert.True(await database.Query<Foo>()
                .AnyAsync("Value = @1", TestContext.Current.CancellationToken, "Milan"));
        }

        [Fact]
        public async Task AnyAsyncReturnsFalseForEmptyResult()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);
            int id = 42;

            Assert.False(await database.Query<Foo>()
                .AnyAsync($"Id = {id}", TestContext.Current.CancellationToken));
            Assert.False(await database.Query<Foo>()
                .Sql("SELECT * FROM Foo WHERE Id > 100")
                .AnyAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task RawSqlQueryFirstOrDefaultAsyncReturnsFirstRow()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Foo foo = await database.Query<Foo>()
                .Sql("SELECT * FROM Foo WHERE Id > 2 ORDER BY Id DESC")
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(foo);
            Assert.Equal(4, foo.Id);
        }

        [Fact]
        public async Task RawSqlQueryAnyAsyncReturnsTrueWhenRowsExist()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Assert.True(await database.Query<Foo>()
                .Sql("SELECT * FROM Foo WHERE Id > 2")
                .AnyAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AsAsyncEnumerableCanBeEnumeratedTwice()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);
            IAsyncEnumerable<Foo> items = database.Query<Foo>()
                .AsAsyncEnumerable(TestContext.Current.CancellationToken);

            var first = new List<int>();
            await foreach (Foo foo in items)
            {
                first.Add(foo.Id);
            }
            var second = new List<int>();
            await foreach (Foo foo in items)
            {
                second.Add(foo.Id);
            }

            Assert.Equal(4, first.Count);
            Assert.Equal(first, second);
        }

        [Fact]
        public async Task FirstOrDefaultAsyncClosesReaderAfterFirstRow()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Foo foo = await database.Query<Foo>()
                .OrderBy("Id")
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, foo.Id);
            Assert.True(IsAnyReaderOpened(database.Connection, Table_Foo));
        }

        [Fact]
        public async Task ToListAsyncThrowsWhenTokenIsAlreadyCancelled()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => database.Query<Foo>().ToListAsync(new CancellationToken(canceled: true)));
        }

        [Fact]
        public async Task ExecuteScalarAsyncReturnsTypedValue()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            int? count = await database.Query<Foo>()
                .Select("COUNT(*)")
                .ExecuteScalarAsync<int>(TestContext.Current.CancellationToken);

            Assert.Equal(4, count);
        }

        [Fact]
        public async Task LinqQueryCanBeMaterializedAsynchronously()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            List<Foo> items = await ((IQueryBase<Foo>)database.Query<Foo>().Where(p => p.Id > 2))
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equivalent(new[] { 3, 4 }, items.Select(p => p.Id));
        }

        [Fact]
        public async Task OrderedLinqQueryFirstOrDefaultAsyncReturnsLastRow()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            Foo foo = await database.Query<Foo>()
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(foo);
            Assert.Equal(4, foo.Id);
        }

        [Fact]
        public async Task ToListAsyncOnNonKormQueryableThrows()
        {
            IQueryable<int> source = new[] { 1 }.AsQueryable();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => source.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AsyncOverloadsBindByNameAndWithoutToken()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);
            CancellationToken ct = TestContext.Current.CancellationToken;

            Assert.Equal("Juraj", (await database.Query<Foo>().FirstOrDefaultAsync("Id = @1", ct, 3)).Value);
            Assert.Equal("Juraj", (await database.Query<Foo>().FirstOrDefaultAsync(
                whereCondition: "Id = @1", cancellationToken: ct, args: new object[] { 3 })).Value);
#pragma warning disable xUnit1051 // overload-binding pins: the omitted token is the point of these calls
            Assert.Equal("Juraj", (await database.Query<Foo>().FirstOrDefaultAsync("Id = @1", args: new object[] { 3 })).Value);
            Assert.True(await database.Query<Foo>().AnyAsync("Id > @1", args: new object[] { 2 }));
            Assert.True(await database.Query<Foo>().AnyAsync($"Id > {2}"));
#pragma warning restore xUnit1051
            Assert.Equal(4, (await database.Query<Foo>().ToListAsync(cancellationToken: ct)).Count);
#pragma warning disable xUnit1051 // overload-binding pins: the omitted token is the point of these calls
            Assert.NotNull(await database.Query<Foo>().FirstOrDefaultAsync());
#pragma warning restore xUnit1051
        }

        [Fact]
        public async Task DatabaseExecuteScalarAsyncReturnsTypedValue()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            int? count = await database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Foo WHERE Id > @1", TestContext.Current.CancellationToken, 1);

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task DatabaseExecuteScalarAsyncReturnsString()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            string value = await database.ExecuteScalarAsync(
                "SELECT Value FROM Foo WHERE Id = @1", TestContext.Current.CancellationToken, 1);

            Assert.Equal("Milan", value);
        }

        [Fact]
        public async Task DatabaseExecuteScalarAsyncReturnsNullForEmptyResult()
        {
            using TestDatabase database = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript);

            int? value = await database.ExecuteScalarAsync<int>(
                "SELECT Id FROM Foo WHERE Id = @1", TestContext.Current.CancellationToken, 42);

            Assert.Null(value);
        }
    }
}
