using Kros.KORM;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace KORM.Test.Performance.Doc
{
    internal class IQueryExample
    {
        public void SqlExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Sql
                var people = database.Query<Person>().Sql(
                    "SELECT p.Id, FirstName, LastName, PostCode " +
                    "FROM Person " +
                    "JOIN Address ON (Person.AddressId = Address.Id) " +
                    "WHERE Age > @1", 18);

                foreach (var person in people)
                {
                    Console.WriteLine(person.FirstName);
                }
                #endregion
            }
        }

        public void SelectExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Select
                var people = database.Query<Person>()
                    .Select("p.Id", "FirstName", "LastName", "PostCode")
                    .From("Person JOIN Address ON (Person.AddressId = Address.Id)")
                    .Where("Age > @1", 18);

                foreach (var person in people)
                {
                    Console.WriteLine(person.FirstName);
                }
                #endregion
            }
        }

        public void LinqExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Linq
                var people = database.Query<Person>()
                    .From("Person JOIN Address ON (Person.AddressId = Address.Id)")
                    .Where(p => p.LastName.EndsWith("ová"))
                    .OrderByDescending(p => p.Id)
                    .Take(5);

                foreach (var person in people)
                {
                    Console.WriteLine(person.FirstName);
                }
                #endregion
            }
        }

        public void Select2Example()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Select2
                var people = database.Query<Person>()
                    .Select(p => new { p.Id, p.FirstName, p.LastName, p.PostCode })
                    .From("Person JOIN Address ON (Person.AddressId = Address.Id)")
                    .Where("Age > @1", 18);

                foreach (var person in people)
                {
                    Console.WriteLine(person.FirstName);
                }
                #endregion
            }
        }

        public void Select11Example()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Select11
                var people = database.Query<Person>().Select("Id, FirstName");
                #endregion
            }
        }

        public void Select12Example()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Select12
                var people = database.Query<Person>().Select("Id", "FirstName");
                #endregion
            }
        }

        public void Select13Example()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Select13
                var people = database.Query<Person>().Select(p => new { p.Id, p.FirstName });
                #endregion
            }
        }

        public void FromExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region From
                var people = database.Query<Person>().From("Person LEFT JOIN Avatar ON (Person.Id = Avatar.PersonId)");
                #endregion
            }
        }

        public void Where1Example()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Where1
                var people = database.Query<Person>().Where("Id < @1 AND Age > @2", 1000, 18);
                #endregion
            }
        }

        public void OrderByExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region OrderBy
                var people = database.Query<Person>().OrderBy("FirstName DESC, LastName");
                #endregion
            }
        }

        public void AnyExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region Any
                var exist = database.Query<Person>().Any("Age > @1", 18);
                #endregion
            }
        }

        public void GroupByExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region GroupBy
                var people = database.Query<Person>().GroupBy("FirstName, LastName");
                #endregion
            }
        }

        public void ExecuteScalarExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region ExecuteScalar
                var id = (int)database.Query<Person>()
                    .Select(p => new { p.Id })
                    .Where("FirstName = @p1 AND LastName = @p2", "Michael", "Štúr")
                    .ExecuteScalar();
                #endregion
            }
        }

        public void ExecuteScalarGenericExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region ExecuteScalarGeneric
                var id = database.Query<Person>()
                    .Select(p => new { p.Id })
                    .Where("FirstName = @p1 AND LastName = @p2", "Michael", "Štúr")
                    .ExecuteScalar<int>();
                #endregion
            }
        }

        public void ExecuteStringScalarExample()
        {
            using (var database = new Database(new SqlConnection()))
            {
                #region ExecuteStringScalar
                var name = database.Query<Person>()
                    .Select(p => new { p.FirstName })
                    .Where("FirstName = @p1 AND LastName = @p2", "Michael", "Štúr")
                    .ExecuteStringScalar();
                #endregion
            }
        }

        public class Person
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string PostCode { get; set; }
        }
    }
}
