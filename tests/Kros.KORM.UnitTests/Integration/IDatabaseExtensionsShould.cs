using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public async Task UpsertRangeAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var pat = new Person { Id = 103, Age = 18, FirstName = "Pat" };
                var mat = new Person { Id = 104, Age = 19, FirstName = "Mat" };
                await database.AddAsync(pat, TestContext.Current.CancellationToken);

                pat.LastName = "Handy";
                mat.LastName = "Handy";

                await database.UpsertRangeAsync(new Person[] { pat, mat }, TestContext.Current.CancellationToken);

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

        private static IEnumerable<Person> GetPeople() =>
        [
            new Person { Id = 3, Age = 30, FirstName = "Ann", LastName = "Lee" },
            new Person { Id = 4, Age = 40, FirstName = "Bob", LastName = "Roe" },
        ];

        [Fact]
        public async Task BulkAddEntitiesAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.BulkAddAsync(GetPeople(), TestContext.Current.CancellationToken);

                Assert.Equal(4, database.Query<Person>().Count());
                Person actual = database.Query<Person>().First(p => p.Id == 3);
                Assert.Equivalent(GetPeople().First(), actual);
            }
        }

        [Fact]
        public async Task BulkAddEntitiesWithOptionsAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await database.BulkAddAsync(GetPeople(), new SqlServerProviderOptions(), TestContext.Current.CancellationToken);

                Assert.Equal(4, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task BulkEditEntitiesAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new List<Person>
                {
                    new Person { Id = 1, Age = 50, FirstName = "Jack", LastName = "Smith" },
                    new Person { Id = 2, Age = 60, FirstName = "Jill", LastName = "Bistrol" },
                };

                await database.BulkEditAsync(people, TestContext.Current.CancellationToken);

                Assert.Equal("Jack", database.Query<Person>().First(p => p.Id == 1).FirstName);
                Assert.Equal(60, database.Query<Person>().First(p => p.Id == 2).Age);
            }
        }

        [Fact]
        public async Task BulkAddAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => database.BulkAddAsync(GetPeople(), new CancellationToken(canceled: true)));

                Assert.Equal(2, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task BulkEditAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new List<Person> { new Person { Id = 1, Age = 50, FirstName = "Jack", LastName = "Smith" } };

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => database.BulkEditAsync(people, new CancellationToken(canceled: true)));

                Assert.Equal("John", database.Query<Person>().First(p => p.Id == 1).FirstName);
            }
        }

        [Fact]
        public async Task AddRangeAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                List<Person> people = GetPeople().ToList();

                await database.AddRangeAsync(people, TestContext.Current.CancellationToken);

                Assert.Equal(4, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task EditRangeAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                Person[] people =
                [
                    new Person { Id = 1, Age = 99, FirstName = "John", LastName = "Smith" },
                    new Person { Id = 2, Age = 99, FirstName = "Kilie", LastName = "Bistrol" },
                ];

                await database.EditRangeAsync(people, TestContext.Current.CancellationToken);

                Assert.All(database.Query<Person>(), p => Assert.Equal(99, p.Age));
            }
        }

        [Fact]
        public async Task DeleteRangeAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                IEnumerable<Person> people = [new Person { Id = 1 }, new Person { Id = 2 }];

                await database.DeleteRangeAsync(people, TestContext.Current.CancellationToken);

                Assert.Empty(database.Query<Person>());
            }
        }

        [Fact]
        public async Task AddRangeAsync_ThrowsWhenTokenAlreadyCancelled()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => database.AddRangeAsync(GetPeople(), new CancellationToken(canceled: true)));

                Assert.Equal(2, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task AddAsync_ThrowsWhenEntityIsCollection()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new HashSet<Person>(GetPeople());

                ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
                    () => database.AddAsync(people, TestContext.Current.CancellationToken));

                Assert.Contains("AddRangeAsync", ex.Message);
                Assert.Equal(2, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task EditAsync_ThrowsWhenEntityIsCollection()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new HashSet<Person>
                {
                    new Person { Id = 1, Age = 99, FirstName = "John", LastName = "Smith" },
                    new Person { Id = 2, Age = 99, FirstName = "Kilie", LastName = "Bistrol" },
                };

                ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
                    () => database.EditAsync(people, TestContext.Current.CancellationToken));

                Assert.Contains("EditRangeAsync", ex.Message);
                Assert.DoesNotContain(database.Query<Person>(), p => p.Age == 99);
            }
        }

        [Fact]
        public async Task EditAsyncWithColumns_ThrowsWhenEntityIsCollection()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new HashSet<Person> { new Person { Id = 1, Age = 99 } };

                ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
                    () => database.EditAsync(
                        entity: people,
                        cancellationToken: TestContext.Current.CancellationToken,
                        columns: new string[] { "Id", "Age" }));

                Assert.Contains("EditRangeAsync", ex.Message);
                Assert.DoesNotContain(database.Query<Person>(), p => p.Age == 99);
            }
        }

        [Fact]
        public async Task DeleteAsync_ThrowsWhenEntityIsCollection()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new HashSet<Person> { new Person { Id = 1 }, new Person { Id = 2 } };

                ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
                    () => database.DeleteAsync(people, TestContext.Current.CancellationToken));

                Assert.Contains("DeleteRangeAsync", ex.Message);
                Assert.Equal(2, database.Query<Person>().Count());
            }
        }

        [Fact]
        public async Task UpsertAsync_ThrowsWhenEntityIsCollection()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript))
            {
                var people = new HashSet<Person>(GetPeople());

                ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
                    () => database.UpsertAsync(people, TestContext.Current.CancellationToken));

                Assert.Contains("UpsertRangeAsync", ex.Message);
                Assert.Equal(2, database.Query<Person>().Count());
            }
        }
    }
}
