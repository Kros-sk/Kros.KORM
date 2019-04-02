using FluentAssertions;
using Kros.KORM.Metadata;
using Kros.KORM.Metadata.Attribute;
using Kros.KORM.Query;
using Kros.KORM.UnitTests.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class PrimaryKeyGenerationTests : DatabaseTestBase
    {
        #region Nested Classes

        [Alias("People")]
        public class Person
        {
            [Key("PK", AutoIncrementMethodType.Indetity)]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class Foo
        {
            [Key("PK", AutoIncrementMethodType.Indetity)]
            public long FooId { get; set; }

            public string Value { get; set; }
        }

        #endregion

        #region SQL Scripts

        private static readonly string CreateTable_People =
$@"CREATE TABLE[dbo].[People] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](50) NULL,

     CONSTRAINT [PK_Person] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]

) ON [PRIMARY];";

        private static readonly string CreateTable_Foo =
$@"CREATE TABLE[dbo].[Foo] (
    [FooId] [bigint] IDENTITY(1,1) NOT NULL,
    [Value] [nvarchar](50) NULL,

     CONSTRAINT [PK_Foo] PRIMARY KEY CLUSTERED ([FooId] ASC) ON [PRIMARY]

) ON [PRIMARY];";

        private static readonly string InsertDataScript =
$@"INSERT INTO People VALUES ('John');
INSERT INTO People VALUES ('Michael');
INSERT INTO People VALUES ('Thomas');";

        #endregion

        [Fact]
        public async Task DbSetShoulFillGeneratedIdsIntoEntities()
        {
            using (var korm = CreateDatabase(CreateTable_People))
            {
                var people = new List<Person>() {
                    new Person() { Name = "Milan" },
                    new Person() { Name = "Peter" }
                };

                IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Add(people);

                await dbSet.CommitChangesAsync();

                people.Select(p => p.Id).Should().BeEquivalentTo(new int[] { 1, 2 });
            }
        }

        [Fact]
        public async Task DbSetShoulFillGeneratedIdsIntoEntitiesWhenTableIsNotEmpty()
        {
            using (var korm = CreateDatabase(CreateTable_People, InsertDataScript))
            {
                var people = new List<Person>() {
                    new Person() { Name = "Milan" },
                    new Person() { Name = "Peter" },
                    new Person() { Name = "Juraj" }
                };

                IDbSet<Person> dbSet = korm.Query<Person>().AsDbSet();
                dbSet.Add(people);

                await dbSet.CommitChangesAsync();

                people.Select(p => p.Id).Should().BeEquivalentTo(new int[] { 4, 5, 6 });
            }
        }

        [Fact]
        public async Task DbSetShoulFillGeneratedIdsIntoEntitiesWhenPrimaryKeyHasDifferentName()
        {
            using (var korm = CreateDatabase(CreateTable_Foo))
            {
                var data = new List<Foo>() {
                    new Foo() { Value = "Bar 1" },
                    new Foo() { Value = "Bar 2" }
                };

                IDbSet<Foo> dbSet = korm.Query<Foo>().AsDbSet();
                dbSet.Add(data);

                await dbSet.CommitChangesAsync();

                data.Select(p => p.FooId).Should().BeEquivalentTo(new int[] { 1, 2 });
            }
        }
    }
}
