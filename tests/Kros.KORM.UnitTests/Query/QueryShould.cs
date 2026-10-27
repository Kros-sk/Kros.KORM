using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Query
{
    public class QueryShould
    {
        [Fact]
        public void CreateDefaultExpression()
        {
            var expression = CreateQuery().Expression;

            Assert.IsType<SelectExpression>(expression);
        }

        [Fact]
        public void CreateExpressionFromSql()
        {
            var expression = (CreateQuery().Sql("Select * from TPerson").Expression as SqlExpression);

            Assert.Equal("Select * from TPerson", expression.Sql);
        }

        [Fact]
        public void CreateExpressionFromInterpolatedSql()
        {
            var query = CreateQuery();
            var expression = (query.Sql($"SELECT * FROM TPerson").Expression as SqlExpression);

            Assert.Equal("SELECT * FROM TPerson", expression.Sql);
        }

        [Fact]
        public void CreateExpressionFromInterpolatedSqlWithParameters()
        {
            var query = CreateQuery();
            var expression = (query.Sql($"SELECT * FROM TPerson WHERE Id = {11}").Expression as SqlExpression);

            Assert.Equal("SELECT * FROM TPerson WHERE Id = @0", expression.Sql);
        }

        [Fact]
        public void CreateExpressionWithColumns()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id, Name").Expression as SelectExpression);

            Assert.Equal("Id, Name", expression.ColumnsExpression.ColumnsPart);
        }

        [Fact]
        public void CreateExpressionWithColumnsFromColumnsList()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id", "Name").Expression as SelectExpression);

            Assert.Equal("Id, Name", expression.ColumnsExpression.ColumnsPart);
        }

        [Fact]
        public void CreateExpressionWithColumnsFromSelector()
        {
            var query = CreateQuery();

            var expression = (query.Select((p) => new { p.Id, p.FirstName }).Expression as SelectExpression);

            Assert.Equal("Id, Name", expression.ColumnsExpression.ColumnsPart);
        }

        [Fact]
        public void CreateExpressionFromComplexQuery()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id, Name, FirstName")
                                   .From("Person as p join Avatar as a on (p.Id = a.PersonId)")
                                   .Where("Id> @1", 0).OrderBy("Name").Expression as SelectExpression);

            Assert.Equal("Id, Name, FirstName", expression.ColumnsExpression.ColumnsPart);
            Assert.Equal("Person as p join Avatar as a on (p.Id = a.PersonId)", expression.TableExpression.TablePart);
            Assert.Equal("Id> @1", expression.WhereExpression.Sql);
            Assert.Equal("Name", expression.OrderByExpression.OrderByPart);
        }

        [Fact]
        public void CreateExpressionWithGroupBy()
        {
            var query = CreateQuery();

            var expression = (query.GroupBy("Name, LastName").Expression as SelectExpression);

            Assert.Equal("Name, LastName", expression.GroupByExpression.GroupByPart);
        }

        [Fact]
        public void CallProviderForExecutingQuery()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.Execute<Person>(Arg.Any<IQuery<Person>>()).Returns(list);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = query.AsEnumerable().FirstOrDefault();

            Assert.Equal(5, person.Id);
        }

        [Fact]
        public void CallProviderForFirstOrDefault()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.Execute<Person>(Arg.Any<IQuery<Person>>()).Returns(list);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = query.FirstOrDefault("Id> @1", 5);

            Assert.Equal(5, person.Id);
        }

        [Fact]
        public void CallProviderForFirstOrDefaultFromInterpolatedString()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.Execute<Person>(Arg.Any<IQuery<Person>>()).Returns(list);

            var id = 5;
            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = query.FirstOrDefault($"Id > {id}");

            Assert.Equal(5, person.Id);
        }

        [Fact]
        public void ReturnTrueFromAnyIfExistItem()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any("Id> @1", 5);

            Assert.True(any);
        }

        [Fact]
        public void ReturnTrueFromAnyIfExistItemFromInterpolatedString()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var id = 5;
            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any($"Id > {id}");

            Assert.True(any);
        }

        [Fact]
        public void ReturnFalseFromAnyIfExistItem()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(null);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any("Id > @1", 5);

            Assert.False(any);
        }

        [Fact]
        public void CallProviderForExecuteScalar()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            Assert.Equal(5, scalar);
        }

        [Fact]
        public void ExecuteScalarReturnNullWhenValueDoesntExist()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(null);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            Assert.False(scalar.HasValue);
        }

        [Fact]
        public void ExecuteScalarReturnNullWhenValueIsDbNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            Assert.False(scalar.HasValue);
        }

        [Fact]
        public void ExecuteScalarReturnHasValueFalseIfProviderReturnNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            Assert.False(scalar.HasValue);
        }

        [Fact]
        public void CallProviderForExecuteStringScalar()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns("Mino");

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteStringScalar();

            Assert.Equal("Mino", scalar);
        }

        [Fact]
        public void ExecuteStringScalarReturnNullIfProviderReturnNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteStringScalar();

            Assert.Null(scalar);
        }

        [Fact]
        public async Task CallProviderForExecutingQueryAsync()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 }, new Person() { Id = 6 } };
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>()).Returns(ToAsync(list));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            List<Person> people = await query.ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(new[] { 5, 6 }, people.Select(p => p.Id));
        }

        [Fact]
        public async Task CallProviderForFirstOrDefaultAsync()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>()).Returns(ToAsync(list));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = await query.FirstOrDefaultAsync("Id > @1", TestContext.Current.CancellationToken, 5);

            Assert.Equal(5, person.Id);
            Assert.Equal("Id > @1", ((SelectExpression)query.Expression).WhereExpression.Sql);
        }

        [Fact]
        public async Task CallProviderForFirstOrDefaultAsyncFromInterpolatedString()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>()).Returns(ToAsync(list));

            var id = 5;
            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = await query.FirstOrDefaultAsync($"Id > {id}", TestContext.Current.CancellationToken);

            Assert.Equal(5, person.Id);
            Assert.Equal("Id > @0", ((SelectExpression)query.Expression).WhereExpression.Sql);
        }

        [Fact]
        public async Task ReturnTrueFromAnyAsyncIfExistItem()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(5));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = await query.AnyAsync("Id > @1", TestContext.Current.CancellationToken, 5);

            Assert.True(any);
            var expression = (SelectExpression)query.Expression;
            Assert.Equal("TOP 1 1", expression.ColumnsExpression.ColumnsPart);
            Assert.Equal("Id > @1", expression.WhereExpression.Sql);
        }

        [Fact]
        public async Task ReturnTrueFromAnyAsyncIfExistItemFromInterpolatedString()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(5));

            var id = 5;
            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = await query.AnyAsync($"Id > {id}", TestContext.Current.CancellationToken);

            Assert.True(any);
            var expression = (SelectExpression)query.Expression;
            Assert.Equal("TOP 1 1", expression.ColumnsExpression.ColumnsPart);
            Assert.Equal("Id > @0", expression.WhereExpression.Sql);
        }

        [Fact]
        public async Task ReturnFalseFromAnyAsyncIfItemDoesNotExist()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(null));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = await query.AnyAsync("Id > @1", TestContext.Current.CancellationToken, 5);

            Assert.False(any);
            Assert.Equal("TOP 1 1", ((SelectExpression)query.Expression).ColumnsExpression.ColumnsPart);
        }

        [Fact]
        public async Task AnyAsyncWithoutConditionReturnsTrueWhenProviderYieldsRow()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(ToAsync(new[] { new Person() { Id = 1 } }));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);

            Assert.True(await query.AnyAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AnyAsyncWithoutConditionReturnsFalseWhenSequenceIsEmpty()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(ToAsync(Array.Empty<Person>()));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);

            Assert.False(await query.AnyAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ExecuteScalarAsyncReturnNullWhenValueIsDbNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(DBNull.Value));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = await query.ExecuteScalarAsync<int>(TestContext.Current.CancellationToken);

            Assert.False(scalar.HasValue);
        }

        [Fact]
        public async Task ExecuteScalarAsyncReturnsValue()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(5));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);

            Assert.Equal(5, await query.ExecuteScalarAsync<int>(TestContext.Current.CancellationToken));
            Assert.Equal(5, await query.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ExecuteStringScalarAsyncReturnNullIfProviderReturnNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>(DBNull.Value));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = await query.ExecuteStringScalarAsync(TestContext.Current.CancellationToken);

            Assert.Null(scalar);
        }

        [Fact]
        public async Task CallProviderForExecuteStringScalarAsync()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteScalarAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<object>("Mino"));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = await query.ExecuteStringScalarAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Mino", scalar);
        }

        [Fact]
        public async Task PassCancellationTokenToProvider()
        {
            using var cts = new CancellationTokenSource();
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            provider.ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), Arg.Any<CancellationToken>())
                .Returns(ToAsync(new[] { new Person() { Id = 1 } }));

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            await query.ToListAsync(cts.Token);

            provider.Received().ExecuteAsync<Person>(Arg.Any<IQuery<Person>>(), cts.Token);
        }

        private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }

        private IQuery<Person> CreateQuery()
        {
            var mapper = new DatabaseMapper(new ConventionModelMapper());
            Query<Person> query = new Query<Person>(mapper,
                new SqlServerQueryProvider(new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(mapper),
                    Substitute.For<IModelBuilder>(),
                    new Logger(),
                    Substitute.For<IDatabaseMapper>()));

            return query;
        }

        [Alias("TPerson")]
        public class Person
        {
            public int Id { get; set; }

            [Alias("Name")]
            public string FirstName { get; set; }

            public string LastName { get; set; }

            [NoMap]
            public int Age { get; set; }
        }
    }
}
