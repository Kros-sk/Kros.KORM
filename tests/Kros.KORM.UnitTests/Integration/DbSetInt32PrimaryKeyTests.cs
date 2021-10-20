using FluentAssertions;
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
    public class DbSetInt32PrimaryKeyTests : DatabaseTestBase
    {
        #region Helpers

        private const string Table_TestTable = "PeopleInt32";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [LastName] [nvarchar](50) NULL
) ON [PRIMARY];";

        [Alias(Table_TestTable)]
        public class Person
        {
            [Key(AutoIncrementMethodType.Custom)]
            public int Id { get; set; }

            public int Age { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [Alias(Table_TestTable)]
        private class Foo
        {
            [Key(AutoIncrementMethodType.None)]
            public int Id { get; set; }
        }

        private TestDatabase CreateTestDatabase()
        {
            TestDatabase db = CreateDatabase(new[] { CreateTable_TestTable });
            foreach (IIdGenerator idGenerator in IdGeneratorFactories.GetGeneratorsForDatabaseInit(db.Connection))
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
                    item.Id.Should().Be(id++);
                }

                var dbItems = korm.Query<Person>().OrderBy(p => p.Id);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                id = 1;
                foreach (var dbItem in dbItems)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    dbItem.Id.Should().Be(id++);
                    dbItem.FirstName.Should().Be(source.FirstName);
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
                    item.Id.Should().Be(id);
                    id += 2;
                }

                var people = korm.Query<Person>().OrderBy(p => p.Id);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                id = 10;
                foreach (var item in people)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    item.Id.Should().Be(id);
                    item.FirstName.Should().Be(source.FirstName);
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

                sourcePeople.Select(p => p.Id).Should().BeEquivalentTo(new int[] { 0, 0, 0 });

                var people = korm.Query<Person>().AsEnumerable();
                people.Select(p => p.Id).Should().BeEquivalentTo(new int[] { 0, 0, 0 });
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
                iterationCount.Should().Be(1);
            }
        }
    }
}
