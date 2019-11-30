using FluentAssertions;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Base;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class IDatabaseExtensionsShould : DatabaseTestBase
    {
        #region Nested Classes

        [Alias("People")]
        private class Person
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }

            public int Age { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        #endregion

        #region SQL Scripts

        private const string Table_TestTable = "People";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [LastName] [nvarchar](50) NULL
) ON [PRIMARY];";

        private static readonly string InsertDataScript =
$@"INSERT INTO {Table_TestTable} VALUES (1, 18, 'John', 'Smith');
INSERT INTO {Table_TestTable} VALUES (2, 22, 'Kilie', 'Bistrol');";

        #endregion

        [Fact]
        public async Task AddEntityAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 3, Age = 18, FirstName = "Bob", LastName = "Bobek" };

                await database.AddAsync(person);

                database.Query<Person>()
                    .FirstOrDefault(p => p.Id == 3)
                    .Should()
                    .BeEquivalentTo(person);
            }
        }

        [Fact]
        public async Task DeleteEntityAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2 };

                await database.DeleteAsync(person);

                database.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByIdAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(2);

                database.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByLinqConditionAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(p => p.Id == 2);

                database.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByConditionAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(condition: "Id = @1", parameters: 2);

                database.Query<Person>()
                    .Should()
                    .NotContain(p => p.Id == 2);
            }
        }

        [Fact]
        public async Task EditEntityAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2, Age = 18, FirstName = "Bob", LastName = "Bobek" };

                await database.EditAsync(person);

                database.Query<Person>()
                    .FirstOrDefault(p => p.Id == 2)
                    .Should()
                    .BeEquivalentTo(person);
            }
        }

        [Fact]
        public async Task EditEntityWithSpecificColumnAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2, Age = 18, FirstName = "Bob", LastName = "Bobek" };

                await database.EditAsync(entity: person, columns: new string[] { "Id", "Age" });

                Person actual = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == 2);

                actual.Age.Should().Be(18);
                actual.FirstName.Should().Be("Kilie");
                actual.LastName.Should().Be("Bistrol");
            }
        }
    }
}
