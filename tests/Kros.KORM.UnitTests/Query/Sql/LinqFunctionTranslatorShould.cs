using FluentAssertions;
using Kros.KORM.Query.Providers;
using Kros.KORM.Query.Sql;
using System;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqFunctionTranslatorShould : LinqTranslatorTestBase
    {
        //  - Neskôr
        //      - Select
        //      - GroupBy
        //      - Join

        // Exceptions when First, Single, SingleOrDefault?

        [Fact]
        public void TranslateWhereMethod()
        {
            var query = Query<Person>().Where(p => p.Id == 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateOrderByMethod()
        {
            var query = Query<Person>().OrderBy(p => p.Id);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id ASC");
        }

        [Fact]
        public void TranslateThenByParamMethod()
        {
            var query = Query<Person>()
                .OrderBy(p => p.Id)
                .ThenBy(p => p.FirstName);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id ASC, FirstName ASC");
        }

        [Fact]
        public void TranslateOrderByDescendingMethod()
        {
            var query = Query<Person>().OrderByDescending(p => p.Id);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id DESC");
        }

        [Fact]
        public void TranslateOrderByDescendingThenByMethod()
        {
            var query = Query<Person>()
                .OrderByDescending(p => p.Id)
                .ThenBy(p => p.FirstName);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id DESC, FirstName ASC");
        }

        [Fact]
        public void TranslateOrderByDescendingThenByDescendingMethod()
        {
            var query = Query<Person>()
                .OrderByDescending(p => p.Id)
                .ThenByDescending(p => p.FirstName);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id DESC, FirstName DESC");
        }

        [Fact]
        public void TranslateOrderByThenByDescendingMethod()
        {
            var query = Query<Person>()
                .OrderBy(p => p.Id)
                .ThenByDescending(p => p.FirstName);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " ORDER BY Id ASC, FirstName DESC");
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
                    "SELECT Id, FirstName, LastName, PostAddress FROM People ORDER BY Id ASC",
                    new LimitOffsetDataReader(0, 10)),
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
                    "SELECT Id, FirstName, LastName, PostAddress FROM People ORDER BY Id ASC",
                    new LimitOffsetDataReader(5, 10)),
                null);
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
                    "SELECT Id, FirstName, LastName, PostAddress FROM People WHERE ((Id > @1)) ORDER BY Id ASC, FirstName DESC",
                    new LimitOffsetDataReader(5, 10)),
                5);
        }

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
        public void TranslateFirstOrDefaultMethod()
        {
            var query = Query<Person>();
            var item = query.FirstOrDefault();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People");
        }

        [Fact]
        public void TranslateFirstOrDefaultWithConditionMethod()
        {
            var query = Query<Person>();
            var item = query.FirstOrDefault(p => p.Id == 5);

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateFirstOrDefaultWithWhereMethod()
        {
            var query = Query<Person>();

            var item = query.Where(p => p.Id == 5).FirstOrDefault();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateFirstMethod()
        {
            var query = Query<Person>();
            var item = query.First();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People");
        }

        [Fact]
        public void TranslateFirstWithConditionMethod()
        {
            var query = Query<Person>();
            var item = query.First(p => p.Id == 5);

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateFirstWithWhereMethod()
        {
            var query = Query<Person>();

            var item = query.Where(p => p.Id == 5).First();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateSingleMethod()
        {
            var query = Query<Person>();
            var item = query.Single();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People");
        }

        [Fact]
        public void TranslateSingleWithConditionMethod()
        {
            var query = Query<Person>();
            var item = query.Single(p => p.Id == 5);

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateSingleWithWhereMethod()
        {
            var query = Query<Person>();

            var item = query.Where(p => p.Id == 5).Single();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateSingleOrDefaultMethod()
        {
            var query = Query<Person>();
            var item = query.SingleOrDefault();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People");
        }

        [Fact]
        public void TranslateSingleOrDefaultWithConditionMethod()
        {
            var query = Query<Person>();
            var item = query.SingleOrDefault(p => p.Id == 5);

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateSingleOrDefaulrWithWhereMethod()
        {
            var query = Query<Person>();

            var item = query.Where(p => p.Id == 5).SingleOrDefault();

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" +
                                       " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateCountMethod()
        {
            var query = Query<Person>();
            var count = query.Count();

            WasGeneratedSameSql(query, "SELECT COUNT(*) FROM People");
        }

        [Fact]
        public void TranslateCountWithConditionMethod()
        {
            var query = Query<Person>();
            var count = query.Count(p => p.Id > 5);

            WasGeneratedSameSql(query, "SELECT COUNT(*) FROM People WHERE ((Id > @1))", 5);
        }

        [Fact]
        public void TranslateSumMethod()
        {
            var query = Query<Person>();
            var sum = query.Sum(p => p.Id);

            WasGeneratedSameSql(query, "SELECT SUM(Id) FROM People");
        }

        [Fact]
        public void TranslateMinMethod()
        {
            var query = Query<Person>();
            var min = query.Min(p => p.Id);

            WasGeneratedSameSql(query, "SELECT MIN(Id) FROM People");
        }

        [Fact]
        public void TranslateMaxMethod()
        {
            var query = Query<Person>();
            var max = query.Max(p => p.Id);

            WasGeneratedSameSql(query, "SELECT MAX(Id) FROM People");
        }

        [Fact]
        public void TranslateAnyMethod()
        {
            var query = Query<Person>();
            var item = query.Any();

            WasGeneratedSameSql(query, @"SELECT (CASE WHEN EXISTS(SELECT '' FROM People) THEN 1 ELSE 0 END)");
        }

        [Fact]
        public void TranslateAnyMethodWithCondition()
        {
            var query = Query<Person>();
            var item = query.Any(p => p.Id > 5);

            WasGeneratedSameSql(query,
                @"SELECT (CASE WHEN EXISTS(SELECT '' FROM People WHERE ((Id > @1))) THEN 1 ELSE 0 END)", 5);
        }

        [Fact]
        public void TranslateAnyAfterWhereMethod()
        {
            var query = Query<Person>();
            var item = query.Where(p => p.Id > 5).Any();

            WasGeneratedSameSql(query,
                            @"SELECT (CASE WHEN EXISTS(SELECT '' FROM People WHERE ((Id > @1))) THEN 1 ELSE 0 END)", 5);
        }

        [Fact]
        public void TranslateQueryWhenUseGenericTypeWithConstraint()
        {
            void TestMethod<T>() where T : IModel
            {
                var query = Query<T>().Where(p => p.Id == 5);
                AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((Id = @1))", 5);
            }

            TestMethod<Person>();
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
    }
}
