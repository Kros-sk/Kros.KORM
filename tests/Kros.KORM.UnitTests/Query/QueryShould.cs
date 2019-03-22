using FluentAssertions;
using Kros.KORM.Helper;
using Kros.KORM.Materializer;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query
{
    public class QueryShould
    {
        [Fact]
        public void CreateDefaultExpression()
        {
            var expression = CreateQuery().Expression;

            expression.Should().BeOfType<SelectExpression>();
        }

        [Fact]
        public void CreateExpressionFromSql()
        {
            var expression = (CreateQuery().Sql("Select * from TPerson").Expression as SqlExpression);

            expression.Sql.Should().Be("Select * from TPerson");
        }

        [Fact]
        public void CreateExpressionFromInterpolatedSql()
        {
            var query = CreateQuery();
            var expression = (query.Sql($"SELECT * FROM TPerson").Expression as SqlExpression);

            expression.Sql.Should().Be("SELECT * FROM TPerson");
        }

        [Fact]
        public void CreateExpressionFromInterpolatedSqlWithParameters()
        {
            var query = CreateQuery();
            var expression = (query.Sql($"SELECT * FROM TPerson WHERE Id = {11}").Expression as SqlExpression);

            expression.Sql.Should().Be("SELECT * FROM TPerson WHERE Id = @0");
        }

        [Fact]
        public void CreateExpressionWithColumns()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id, Name").Expression as SelectExpression);

            expression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name");
        }

        [Fact]
        public void CreateExpressionWithColumnsFromColumnsList()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id", "Name").Expression as SelectExpression);

            expression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name");
        }

        [Fact]
        public void CreateExpressionWithColumnsFromSelector()
        {
            var query = CreateQuery();

            var expression = (query.Select((p) => new { p.Id, p.FirstName }).Expression as SelectExpression);

            expression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name");
        }

        [Fact]
        public void CreateExpressionFromComplexQuery()
        {
            var query = CreateQuery();

            var expression = (query.Select("Id, Name, FirstName")
                                   .From("Person as p join Avatar as a on (p.Id = a.PersonId)")
                                   .Where("Id> @1", 0).OrderBy("Name").Expression as SelectExpression);

            expression.ColumnsExpression.ColumnsPart.Should().Be("Id, Name, FirstName");
            expression.TableExpression.TablePart.Should().Be("Person as p join Avatar as a on (p.Id = a.PersonId)");
            expression.WhereExpression.Sql.Should().Be("Id> @1");
            expression.OrderByExpression.OrderByPart.Should().Be("Name");
        }

        [Fact]
        public void CreateExpressionWithGroupBy()
        {
            var query = CreateQuery();

            var expression = (query.GroupBy("Name, LastName").Expression as SelectExpression);

            expression.GroupByExpression.GroupByPart.Should().Be("Name, LastName");
        }

        [Fact]
        public void CallProviderForExecutingQuery()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.Execute<Person>(Arg.Any<IQuery<Person>>()).Returns(list);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = query.AsEnumerable().FirstOrDefault();

            person.Id.Should().Be(5);
        }

        [Fact]
        public void CallProviderForFirstOrDefault()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();
            List<Person> list = new List<Person>() { new Person() { Id = 5 } };
            provider.Execute<Person>(Arg.Any<IQuery<Person>>()).Returns(list);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var person = query.FirstOrDefault("Id> @1", 5);

            person.Id.Should().Be(5);
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

            person.Id.Should().Be(5);
        }

        [Fact]
        public void ReturnTrueFromAnyIfExistItem()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any("Id> @1", 5);

            any.Should().BeTrue();
        }

        [Fact]
        public void ReturnTrueFromAnyIfExistItemFromInterpolatedString()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var id = 5;
            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any($"Id > {id}");

            any.Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseFromAnyIfExistItem()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(null);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var any = query.Any("Id > @1", 5);

            any.Should().BeFalse();
        }

        [Fact]
        public void CallProviderForExecuteScalar()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(5);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            scalar.Should().Be(5);
        }

        [Fact]
        public void ExecuteScalarReturnNullWhenValueDoesntExist()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(null);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            scalar.HasValue.Should().BeFalse();
        }

        [Fact]
        public void ExecuteScalarReturnNullWhenValueIsDbNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            scalar.HasValue.Should().BeFalse();
        }

        [Fact]
        public void ExecuteScalarReturnHasValueFalseIfProviderReturnNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteScalar<int>();

            scalar.HasValue.Should().BeFalse();
        }

        [Fact]
        public void CallProviderForExecuteStringScalar()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns("Mino");

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteStringScalar();

            scalar.Should().Be("Mino");
        }

        [Fact]
        public void ExecuteStringScalarReturnNullIfProviderReturnNull()
        {
            var provider = Substitute.For<KORM.Query.IQueryProvider>();

            provider.ExecuteScalar<Person>(Arg.Any<IQuery<Person>>()).Returns(DBNull.Value);

            var query = new Query<Person>(new DatabaseMapper(new ConventionModelMapper()), provider);
            var scalar = query.ExecuteStringScalar();

            scalar.Should().BeNull();
        }

        private IQuery<Person> CreateQuery()
        {
            var mapper = new DatabaseMapper(new ConventionModelMapper());
            Query<Person> query = new Query<Person>(mapper,
                new SqlServerQueryProvider(new SqlConnection(),
                    new SqlServerSqlExpressionVisitorFactory(mapper),
                    Substitute.For<IModelBuilder>(),
                    new Logger()));

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
