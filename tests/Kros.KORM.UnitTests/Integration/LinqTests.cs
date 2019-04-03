using FluentAssertions;
using Kros.KORM.UnitTests.Base;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class LinqTests : DatabaseTestBase
    {
        #region Nested Classes

        private class Foo
        {
            public int Id { get; set; }
        }

        #endregion

        #region SQL Scripts

        private static readonly string CreateTable_FooTable =
$@"CREATE TABLE [dbo].[Foo] (
    [Id] [int] NOT NULL
) ON [PRIMARY];";

        private static readonly string InsertIntoFooScript =
$@"INSERT INTO [Foo] VALUES (1);
INSERT INTO [Foo] VALUES (2);
INSERT INTO [Foo] VALUES (3);
INSERT INTO [Foo] VALUES (4);";

        #endregion

        [Fact]
        public void QueryShouldReturnZeroWhenSumEmptyResultSet()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            {
                var sum = korm.Query<Foo>()
                    .Where(p => p.Id == -1)
                    .Sum(p => p.Id);

                sum.Should().Be(0);
            }
        }
    }
}
