using Kros.KORM.Metadata.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqTranslatorShould : LinqTranslatorTestBase
    {
        [Theory]
        [MemberData(nameof(TranslateBooleanConditionsData))]
        public void TranslateBooleanConditions(Expression<Func<DeleteDto, bool>> condition, string expectedWhere)
        {
            string expectedQuery = $"SELECT Id, IsDeleted FROM DeleteDto WHERE ({expectedWhere})";
            var query = Query<DeleteDto>().Where(condition);
            AreSame(query, expectedQuery);
        }

        public static IEnumerable<object[]> TranslateBooleanConditionsData()
        {
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => p.IsDeleted),
                "(IsDeleted = 1)"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => !p.IsDeleted),
                "(IsDeleted <> 1)"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => (p.IsDeleted == true) && p.IsDeleted),
                "((IsDeleted = 1) AND (IsDeleted = 1))"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => (p.IsDeleted == false) || !p.IsDeleted),
                "((IsDeleted = 0) OR (IsDeleted <> 1))"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => p.IsDeleted || (p.IsDeleted == true)),
                "((IsDeleted = 1) OR (IsDeleted = 1))"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => !p.IsDeleted && (p.IsDeleted == false)),
                "((IsDeleted <> 1) AND (IsDeleted = 0))"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => (false == p.IsDeleted) && !p.IsDeleted),
                "((0 = IsDeleted) AND (IsDeleted <> 1))"
            };
            yield return new object[]
            {
                (Expression<Func<DeleteDto, bool>>)(p => (true == p.IsDeleted) && p.IsDeleted),
                "((1 = IsDeleted) AND (IsDeleted = 1))"
            };
        }

        [Fact]
        public void EvaluateLambdaAndTranslateItToSql()
        {
            var firstName = "John";
            Func<string> lastName = () => "Smith";

            var query = Query<Person>()
                .Where(p => p.FirstName == firstName &&
                            p.LastName == lastName() &&
                            p.Id == GetId(2));

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((((FirstName = @1) AND (LastName = @2)) AND (Id = @3)))",
                    "John", "Smith", 4);
        }

        [Fact]
        public void TranslateQuotesPriorityInCondition()
        {
            var query = Query<Person>()
                .Where(p => p.FirstName == "John" ||
                            (p.LastName == "Smith" &&
                            p.Id == 5));

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE (((FirstName = @1) OR ((LastName = @2) AND (Id = @3))))",
                    "John", "Smith", 5);
        }

        [Fact]
        public void MapPropertyNameToColumnName()
        {
            var query = Query<Person>()
                .Where(p => p.Address == "Zilina");

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((PostAddress = @1))", "Zilina");
        }

        [Fact]
        public void TranslateMultiLinqMethodCombination()
        {
            var query = Query<Person>()
                .Where(p => p.Address == "Zilina")
                .OrderBy(p => p.Id);

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((PostAddress = @1))" +
                           " ORDER BY Id ASC", "Zilina");
        }

        [Fact]
        public void TranslateCombinationOfQueryBuilderAndLinq()
        {
            var query = Query<Person>()
                .Select("PostAddress")
                .From("Person JON Address ON (Person.AddressId == Address.Id)")
                .Where(p => p.Id == 5);

            AreSame(query, "SELECT PostAddress" +
                           " FROM Person JON Address ON (Person.AddressId == Address.Id)" +
                           " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateWhereConditionWithGuid()
        {
            var id = new Guid("cc816707-2749-49a2-8b32-8f6d94250d1e");
            var query = Query<Foo>()
                .Where(p => p.Id == id);

            AreSame(query, "SELECT Id" +
                           " FROM Foo" +
                           " WHERE ((Id = @1))", id);
        }

        private int GetId(int id)
        {
            return id * 2;
        }

        [Alias("People")]
        public new class Person
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [Alias("PostAddress")]
            public string Address { get; set; }
        }

        public class DeleteDto
        {
            public int Id { get; set; }
            public bool IsDeleted { get; set; }
        }

        class Foo
        {
            public Guid Id { get; set; }
        }
    }
}
