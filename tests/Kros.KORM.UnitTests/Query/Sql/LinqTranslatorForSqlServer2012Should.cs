using FluentAssertions;
using Kros.KORM.Query.Sql;
using System;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqTranslatorForSqlServer2012Should : LinqTranslatorTestBase
    {
        #region Tests

        [Fact]
        public void ThrowInvalidOperationExceptionWhenUsedSkipWithoutOrderBy()
        {
            var visitor = CreateVisitor();
            var query = Query<Person>()
                .Skip(10);
            Action action = () => visitor.GenerateSql(query.Expression);

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TranslateTakeMethod()
        {
            var query = Query<Person>()
                .Take(5);

            AreSame(query, "SELECT TOP 5 Id, FirstName, LastName, PostAddress FROM People");
        }

        [Fact]
        public void TranslateSkipMethod()
        {
            var query = Query<Person>()
                .Skip(10)
                .OrderBy(p => p.Id);

            AreSame(
                query,
                new QueryInfo(
                    "SELECT Id, FirstName, LastName, PostAddress FROM People ORDER BY Id ASC OFFSET 10 ROWS",
                    null),
                null);
        }

        [Fact]
        public void TranslateSkipWithTakeMethod()
        {
            var query = Query<Person>()
                .Skip(10)
                .Take(5)
                .OrderBy(p => p.Id);

            AreSame(
                query,
                new QueryInfo(
                    "SELECT Id, FirstName, LastName, PostAddress FROM People ORDER BY Id ASC " +
                    "OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY",
                    null),
                null);
        }

        [Fact]
        public void TranslateSkipMethodWithCondition()
        {
            var query = Query<Person>()
                .Where(p => p.Id > 5)
                .Skip(10)
                .OrderBy(p => p.Id);

            AreSame(
                query,
                new QueryInfo(
                    "SELECT Id, FirstName, LastName, PostAddress FROM People WHERE ((Id > @1)) ORDER BY Id ASC OFFSET 10 ROWS",
                    null),
                5);
        }

        [Fact]
        public void TranslateSkipWithTakeMethodAndCondition()
        {
            var query = Query<Person>()
                .Where(p => p.Id > 5)
                .Skip(10)
                .Take(5)
                .OrderBy(p => p.Id);

            AreSame(
                query,
                new QueryInfo(
                    "SELECT Id, FirstName, LastName, PostAddress FROM People WHERE ((Id > @1)) ORDER BY Id ASC " +
                    "OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY",
                    null),
                5);
        }

        [Fact]
        public void TranslateSkipWithTakeAndConditionAndComplexOrderBy()
        {
            var query = Query<Person>()
                .Where(p => p.Id > 5)
                .Skip(10)
                .Take(5)
                .OrderBy(p => p.Id)
                .ThenByDescending(p => p.FirstName);

            AreSame(
                query,
                new QueryInfo(
                    "SELECT Id, FirstName, LastName, PostAddress FROM People WHERE ((Id > @1)) " +
                    "ORDER BY Id ASC, FirstName DESC " +
                    "OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY",
                    null),
                5);
        }

        [Fact]
        public void CreateCorrectOrderByIfItIsSpecifiedByStringAndAlsoByExpression()
        {
            var query = Query<Person>()
                .OrderBy("Id DESC")
                .OrderByDescending(p => p.FirstName)
                .OrderBy(p => p.LastName);

            AreSame(
                query,
                "SELECT Id, FirstName, LastName, PostAddress FROM People ORDER BY Id DESC, FirstName DESC, LastName ASC");
        }

        [Fact]
        public void NotThrowWhenUsedSkipWithStringOrderBy()
        {
            var visitor = CreateVisitor();
            var query = Query<Person>()
                .OrderBy("Id DESC")
                .Skip(10);
            Action action = () => visitor.GenerateSql(query.Expression);

            action.Should().NotThrow();
        }

        #endregion

        #region Helpers

        protected override ISqlExpressionVisitor CreateVisitor() => new SqlServer2012SqlGenerator(Database.DatabaseMapper);

        #endregion
    }
}
