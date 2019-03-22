using Kros.KORM.Metadata.Attribute;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqStringFuncionTranslatorShould : LinqTranslatorTestBase
    {
        //Podporované funkcie
        //  * StartsWith
        //  * EndWiths
        //  * Contains
        //  * IsNullOrEmpty
        //  * ToUpper
        //  * ToLower
        //  * Replace
        //  * SubString
        //  * Trim
        //V budúcnosti
        //  / IndexOf
        //  / Remove
        //  / Concat

        [Fact]
        public void TranslateStartWithMethod()
        {
            var query = Query<Person>().Where(p => p.Name.StartsWith("Joh"));

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((FirstName LIKE @1 + '%'))", "Joh");
        }

        [Fact]
        public void TranslateEndWithMethod()
        {
            var query = Query<Person>().Where(p => p.LastName.EndsWith("ová"));

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((LastName LIKE '%' + @1))", "ová");
        }

        [Fact]
        public void TranslateContainsMethod()
        {
            var query = Query<Person>().Where(p => p.LastName.Contains("oh"));

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((LastName LIKE '%' + @1 + '%'))", "oh");
        }

        [Fact]
        public void TranslateIsNullOrEmptyMethod()
        {
            var query = Query<Person>().Where(p => string.IsNullOrEmpty(p.LastName));

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((LastName IS NULL OR LastName = ''))");
        }

        [Fact]
        public void TranslateToUpperMethod()
        {
            var query = Query<Person>().Where(p => p.Name.ToUpper() == "JOHN");

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((UPPER(FirstName) = @1))", "JOHN");
        }

        [Fact]
        public void TranslateToLowerMethod()
        {
            var query = Query<Person>().Where(p => "john" == p.Name.ToLower());

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((@1 = LOWER(FirstName)))", "john");
        }

        [Fact]
        public void TranslateReplaceMethod()
        {
            var query = Query<Person>().Where(p => p.Name.Replace("hn", "zo") == "Jozo");

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((REPLACE(FirstName, @1, @2) = @3))", "hn", "zo", "Jozo");
        }

        [Fact]
        public void TranslateSubstringMethod()
        {
            var query = Query<Person>().Where(p => p.Name.Substring(1, 2) == "oh");

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                            " WHERE ((SUBSTRING(FirstName, @1 + 1, @2) = @3))", 1, 2, "oh");
        }

        [Fact]
        public void TranslateSubstringToEndMethod()
        {
            var query = Query<Person>().Where(p => p.Name.Substring(2) == "hn");

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((SUBSTRING(FirstName, @1 + 1, @2) = @3))", 2, 8000, "hn");
        }

        [Fact]
        public void TranslateTrimEndMethod()
        {
            var query = Query<Person>().Where(p => p.Name.Trim() == "John");

            AreSame(query, "SELECT Id, FirstName, LastName FROM People" +
                           " WHERE ((RTRIM(LTRIM(FirstName)) = @1))", "John");
        }

        [Alias("People")]
        public new class Person
        {
            public int Id { get; set; }
            [Alias("FirstName")]
            public string Name { get; set; }
            public string LastName { get; set; }
        }
    }
}
