using Kros.KORM.Metadata.Attribute;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqUnaryOperatorTranslatorShould : LinqTranslatorTestBase
    {
        [Fact]
        public void TranslateNotOperator()
        {
            var query = Query<Person>().Where(p => !(p.Id == 5));

            AreSame(query, "SELECT Id, WorkerType FROM People" +
                           " WHERE (NOT (Id = @1))", 5);
        }

        [Fact]
        public void TranslateEnumCondition()
        {
            var query = Query<Person>().Where(p => p.WorkerType == WorkerType.Manager);

            AreSame(query, "SELECT Id, WorkerType FROM People" +
                           " WHERE ((WorkerType = @1))", 2);
        }

        [Alias("People")]
        public new class Person
        {
            public int Id { get; set; }

            public WorkerType WorkerType { get; set; }
        }

        public enum WorkerType
        {
            Worker = 1,
            Manager = 2
        }
    }
}