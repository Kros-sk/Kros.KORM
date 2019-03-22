using Kros.KORM.Metadata.Attribute;
using System;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqTranslatorShould : LinqTranslatorTestBase
    {
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
                .OrderBy(p=> p.Id);

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

        class Foo
        {
            public Guid Id { get; set; }
        }
    }
}
