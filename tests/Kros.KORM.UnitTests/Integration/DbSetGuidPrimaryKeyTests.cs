using FluentAssertions;
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
    public class DbSetGuidPrimaryKeyTests : DatabaseTestBase
    {
        #region Helpers

        private const string Table_TestTable = "PeopleGuid";

        private static readonly string CreateTable_TestTable =
$@"CREATE TABLE [dbo].[{Table_TestTable}] (
    [Id] [uniqueidentifier] NOT NULL,
    [Age] [int] NULL,
    [FirstName] [nvarchar](50) NULL,
    [LastName] [nvarchar](50) NULL
) ON [PRIMARY];";

        [Alias(Table_TestTable)]
        public class Person
        {
            [Key(AutoIncrementMethodType.Custom)]
            public Guid Id { get; set; }

            public int Age { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [Alias(Table_TestTable)]
        private class Foo
        {
            [Key(AutoIncrementMethodType.None)]
            public Guid Id { get; set; }
        }

        private TestDatabase CreateTestDatabase()
            => CreateDatabase(new[] { CreateTable_TestTable });

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

                foreach (var item in sourcePeople)
                {
                    item.Id.Should().NotBeEmpty();
                }

                var dbItems = korm.Query<Person>().OrderBy(p => p.FirstName);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                foreach (var dbItem in dbItems)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    dbItem.FirstName.Should().Be(source.FirstName);
                }
            }
        }

        [Fact]
        public void DoNotGeneratePrimaryKeyIfFilled()
        {
            using (var korm = CreateTestDatabase())
            {
                var guids = new[]
                {
                    Guid.Parse("82f2532b-afe8-4da7-9fc5-386e3ee2276e"),
                    Guid.Parse("a926df7f-5d33-41d6-b6f2-92d6fb10760f"),
                    Guid.Parse("3047d1e5-af49-40e0-8850-9cb6a665e96c")
                };
                var dbSet = korm.Query<Person>().AsDbSet();
                var sourcePeople = new List<Person> {
                    new Person() { Id = guids[0], FirstName = "Alice" },
                    new Person() { Id = guids[1], FirstName = "Bob" },
                    new Person() { Id = guids[2], FirstName = "Connor" }
                };

                dbSet.Add(sourcePeople);
                dbSet.CommitChanges();

                for (var i = 0; i < sourcePeople.Count; i++)
                {
                    sourcePeople[i].Id.Should().Be(guids[i]);
                }

                var people = korm.Query<Person>().OrderBy(p => p.FirstName);
                var sourceEnumerator = sourcePeople.GetEnumerator();
                foreach (var item in people)
                {
                    sourceEnumerator.MoveNext();
                    var source = sourceEnumerator.Current;

                    item.Id.Should().Be(source.Id);
                    item.FirstName.Should().Be(source.FirstName);
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

                sourcePeople.Select(p => p.Id).Should().BeEquivalentTo(new[] { Guid.Empty, Guid.Empty, Guid.Empty });

                var people = korm.Query<Person>().AsEnumerable();
                people.Select(p => p.Id).Should().BeEquivalentTo(new[] { Guid.Empty, Guid.Empty, Guid.Empty });
            }
        }
    }
}
