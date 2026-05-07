using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.UnitTests.Base;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public partial class IDatabaseExtensionsShould(KormTestsFixture kormContext) : DatabaseTestBase(kormContext)
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

                await database.AddAsync(person, TestContext.Current.CancellationToken);

                Assert.Equivalent(person, database.Query<Person>()
                    .FirstOrDefault(p => p.Id == 3));
            }
        }

        [Fact]
        public async Task DeleteEntityAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2 };

                await database.DeleteAsync(person, TestContext.Current.CancellationToken);

                Assert.DoesNotContain(database.Query<Person>(), p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByIdAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(2, TestContext.Current.CancellationToken);

                Assert.DoesNotContain(database.Query<Person>(), p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByLinqConditionAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(p => p.Id == 2, TestContext.Current.CancellationToken);

                Assert.DoesNotContain(database.Query<Person>(), p => p.Id == 2);
            }
        }

        [Fact]
        public async Task DeleteEntityByConditionAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.DeleteAsync<Person>(condition: "Id = @1", TestContext.Current.CancellationToken, parameters: 2);

                Assert.DoesNotContain(database.Query<Person>(), p => p.Id == 2);
            }
        }

        [Fact]
        public async Task EditEntityAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2, Age = 18, FirstName = "Bob", LastName = "Bobek" };

                await database.EditAsync(person, TestContext.Current.CancellationToken);

                Assert.Equivalent(person, database.Query<Person>()
                    .FirstOrDefault(p => p.Id == 2));
            }
        }

        [Fact]
        public async Task EditEntityWithSpecificColumnAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person() { Id = 2, Age = 18, FirstName = "Bob", LastName = "Bobek" };

                await database.EditAsync(entity: person, cancellationToken: TestContext.Current.CancellationToken, columns: new string[] { "Id", "Age" });

                Person actual = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == 2);

                Assert.Equal(18, actual.Age);
                Assert.Equal("Kilie", actual.FirstName);
                Assert.Equal("Bistrol", actual.LastName);
            }
        }

        [Fact]
        public async Task InsertEntityWithUpsertCommand()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person { Id = 101, Age = 18, FirstName = "Bob", LastName = "Bobek" };
                await database.UpsertAsync(person, TestContext.Current.CancellationToken);

                Person actual = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == person.Id);

                Assert.Equal(18, actual.Age);
                Assert.Equal("Bob", actual.FirstName);
                Assert.Equal("Bobek", actual.LastName);
            }
        }

        [Fact]
        public async Task UpdateExistingEntityWithUpsertCommand()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var person = new Person { Id = 102, Age = 18, FirstName = "Bob", LastName = "Bobek" };
                await database.AddAsync(person, TestContext.Current.CancellationToken);

                person = new Person { Id = 102, Age = 99, FirstName = "Marlyn", LastName = "Manson" };
                await database.UpsertAsync(person, cancellationToken: TestContext.Current.CancellationToken, columns: new string[] { "Id", "Age" });

                Person actual = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == person.Id);

                Assert.Equal(99, actual.Age);
                Assert.Equal("Bob", actual.FirstName);
                Assert.Equal("Bobek", actual.LastName);
            }
        }

        [Fact]
        public async Task UpsertMultipleEntities()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var pat = new Person { Id = 103, Age = 18, FirstName = "Pat" };
                var mat = new Person { Id = 104, Age = 19, FirstName = "Mat" };
                await database.AddAsync(pat, TestContext.Current.CancellationToken);

                pat.LastName = "Handy";
                mat.LastName = "Handy";

                await database.UpsertAsync<Person>(new Person[] { pat, mat }, TestContext.Current.CancellationToken);

                Person actualPat = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == pat.Id);
                Person actualMat = database
                    .Query<Person>()
                    .FirstOrDefault(p => p.Id == mat.Id);

                Assert.Equal(18, actualPat.Age);
                Assert.Equal("Pat", actualPat.FirstName);
                Assert.Equal("Handy", actualPat.LastName);

                Assert.Equal(19, actualMat.Age);
                Assert.Equal("Mat", actualMat.FirstName);
                Assert.Equal("Handy", actualMat.LastName);
            }
        }
    }
}
