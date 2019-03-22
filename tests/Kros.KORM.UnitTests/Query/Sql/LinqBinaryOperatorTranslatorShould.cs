using Kros.KORM.Metadata.Attribute;
using System;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqBinaryOperatorTranslatorShould : LinqTranslatorTestBase
    {
        [Fact]
        public void TranslateAndAlsoOperator()
        {
            var query = Query<Person>().Where(p => p.Id == 5 && p.Name == "John");

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id = @1) AND (FirstName = @2)))", 5, "John");
        }

        [Fact]
        public void TranslateAndOperator()
        {
            var query = Query<Person>().Where(p => p.Id == 5 & p.Name == "John");

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id = @1) AND (FirstName = @2)))", 5, "John");
        }

        [Fact]
        public void TranslateOrElseOperator()
        {
            var query = Query<Person>().Where(p => p.Id == 5 || p.Name == "John");

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id = @1) OR (FirstName = @2)))", 5, "John");
        }

        [Fact]
        public void TranslateOrOperator()
        {
            var query = Query<Person>().Where(p => p.Id == 5 | p.Name == "John");

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id = @1) OR (FirstName = @2)))", 5, "John");
        }

        [Fact]
        public void TranslateEqualOperator()
        {
            var query = Query<Person>().Where(p => p.Id == 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id = @1))", 5);
        }

        [Fact]
        public void TranslateNotEqualOperator()
        {
            var query = Query<Person>().Where(p => p.Id != 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id <> @1))", 5);
        }

        [Fact]
        public void TranslateLessThanOperator()
        {
            var query = Query<Person>().Where(p => p.Id < 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id < @1))", 5);
        }

        [Fact]
        public void TranslateLessThanOrEqualOperator()
        {
            var query = Query<Person>().Where(p => p.Id <= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id <= @1))", 5);
        }

        [Fact]
        public void TranslateGreaterThanOperator()
        {
            var query = Query<Person>().Where(p => p.Id > 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id > @1))", 5);
        }

        [Fact]
        public void TranslateGreaterThanOrEqualOperator()
        {
            var query = Query<Person>().Where(p => p.Id >= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((Id >= @1))", 5);
        }

        [Fact]
        public void TranslateAddOperator()
        {
            var query = Query<Person>().Where(p => p.Id + 1 >= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id + @1) >= @2))", 1, 5);
        }

        [Fact]
        public void TranslateSubtractOperator()
        {
            var query = Query<Person>().Where(p => p.Id - 1 >= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id - @1) >= @2))", 1, 5);
        }

        [Fact]
        public void TranslateMultiplyOperator()
        {
            var query = Query<Person>().Where(p => p.Id * 1 >= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id * @1) >= @2))", 1, 5);
        }

        [Fact]
        public void TranslateDivideOperator()
        {
            var query = Query<Person>().Where(p => p.Id /1  >= 5);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE (((Id / @1) >= @2))", 1, 5);
        }

        [Fact]
        public void TranslateIsNullOperator()
        {
            var query = Query<Person>().Where(p => p.PaymentId == null);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((PaymentId IS NULL))");
        }

        [Fact]
        public void TranslateIsNotNullOperator()
        {
            var query = Query<Person>().Where(p => p.PaymentId != null);

            AreSame(query, "SELECT Id, FirstName, LastName, PaymentId FROM People" +
                           " WHERE ((PaymentId IS NOT NULL))");
        }

        [Alias("People")]
        public new class Person
        {
            public int Id { get; set; }

            [Alias("FirstName")]
            public string Name { get; set; }
            public string LastName { get; set; }
            public int? PaymentId { get; set; }
        }
    }
}