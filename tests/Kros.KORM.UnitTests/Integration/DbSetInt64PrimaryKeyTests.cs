using Kros.Data;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class DbSetInt64PrimaryKeyTests(KormTestsFixture kormContext) : DatabaseTestBase(kormContext)
    {
        #region Helpers

        private const string Table_TestTable = "PeopleInt64";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [bigint] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [LastName] [nvarchar](50) NULL
) ON [PRIMARY];";

        [Alias(Table_TestTable)]
        public class Person
        {
            [Key(AutoIncrementMethodType.Custom)]
            public long Id { get; set; }

            public int Age { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [Alias(Table_TestTable)]
        private class Foo
        {
            [Key(AutoIncrementMethodType.None)]
            public long Id { get; set; }
        }

        private TestDatabase CreateTestDatabase()
        {
            TestDatabase db = CreateDatabase(new[] { CreateTable_TestTable });
            using IIdGeneratorsForDatabaseInit idGenerators = IdGeneratorFactories.GetGeneratorsForDatabaseInit(db.Connection);
            foreach (IIdGenerator idGenerator in idGenerators)
            {
                idGenerator.InitDatabaseForIdGenerator();
            }
            return db;
        }

        #endregion

        [Fact]
        public void GeneratePrimaryKey() => GeneratePrimaryKeyCore(dbSet => dbSet.CommitChanges());

        [Fact]
        public void GeneratePrimaryKeyBulkInsert() => GeneratePrimaryKeyCore(dbSet => dbSet.BulkInsert());

        private void GeneratePrimaryKeyCore(Action<IDbSet<Person>> commitAction)
        {
            using (var korm = CreateTestDatabase())
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var sourcePeople = new List<Person>
                {
                    new Person() { FirstName = "Alice" },
                    new Person() { FirstName = "Bob" },
                    new Person() { FirstName = "Connor" }
                };

                dbSet.Add(sourcePeople);
                commitAction(dbSet);

                var id = 1;
                foreach (var item in sourcePeople)
                {
                    Assert.Equal(id++, item.Id);
                }

                var dbItems = korm.Query<Person>().OrderBy(p => p.Id);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                id = 1;
                foreach (var dbItem in dbItems)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    Assert.Equal(id++, dbItem.Id);
                    Assert.Equal(source.FirstName, dbItem.FirstName);
                }
            }
        }

        [Fact]
        public void DoNotGeneratePrimaryKeyIfFilled()
        {
            using (var korm = CreateTestDatabase())
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var sourcePeople = new List<Person> {
                    new Person() { Id = 10, FirstName = "Alice" },
                    new Person() { Id = 12, FirstName = "Bob" },
                    new Person() { Id = 14, FirstName = "Connor" }
                };

                dbSet.Add(sourcePeople);
                dbSet.CommitChanges();

                var id = 10;
                foreach (var item in sourcePeople)
                {
                    Assert.Equal(id, item.Id);
                    id += 2;
                }

                var people = korm.Query<Person>().OrderBy(p => p.Id);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                id = 10;
                foreach (var item in people)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    Assert.Equal(id, item.Id);
                    Assert.Equal(source.FirstName, item.FirstName);
                    id += 2;
                }
            }
        }

        [Fact]
        public void DoNotGeneratePrimaryKeyIfKeyIsNotAutoIncrement()
        {
            using (var korm = CreateTestDatabase())
            {
                var dbSet = korm.Query<Foo>().AsDbSet();
                var sourcePeople = new List<Foo>() {
                    new Foo(),
                    new Foo(),
                    new Foo(),
                };

                dbSet.Add(sourcePeople);
                dbSet.CommitChanges();

                Assert.Equivalent(new int[] { 0, 0, 0 }, sourcePeople.Select(p => p.Id));

                var people = korm.Query<Person>().AsEnumerable();
                Assert.Equivalent(new int[] { 0, 0, 0 }, people.Select(p => p.Id));
            }
        }

        [Fact]
        public void IteratedThroughItemsOnlyOnceWhenGeneratePrimaryKeys()
        {
            using (var korm = CreateTestDatabase())
            {
                var dbSet = korm.Query<Person>().AsDbSet();
                var iterationCount = 0;
                IEnumerable<Person> SourceItems()
                {
                    iterationCount++;
                    yield return new Person() { Id = 5, FirstName = "Alice" };
                }
                var sourcePeople = SourceItems();

                dbSet.BulkInsert(sourcePeople);
                Assert.Equal(1, iterationCount);
            }
        }
    }
}
