using FluentAssertions;
using Kros.KORM.Metadata;
using Kros.KORM.UnitTests.Base;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class QueryFilterShould : DatabaseTestBase
    {
        #region Nested Classes

        private class Foo
        {
            public int Id { get; set; }

            public int UserId { get; set; }

            public string Value { get; set; }
        }

        #endregion

        #region SQL Scripts

        private static readonly string CreateTable_FooTable =
$@"CREATE TABLE [dbo].[Foo] (
    [Id] [int] NOT NULL,
    [UserId] [int],
    [Value] [nvarchar](255)
) ON [PRIMARY];";

        private static readonly string InsertIntoFooScript =
$@"INSERT INTO [Foo] VALUES (1, 1, 'Milan');
INSERT INTO [Foo] VALUES (2, 1, 'Peter');
INSERT INTO [Foo] VALUES (3, 2, 'Juraj');
INSERT INTO [Foo] VALUES (4, 3, 'Jakub');";

        #endregion

        [Fact]
        public void ApplyQueryFilterToCondition()
        {
            using (TestDatabase test = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            using (IDatabase database = CreateDatabase(test))
            {
                var ids = database.Query<Foo>()
                    .Where(p => p.Id > 1)
                    .ToList()
                    .Select(p => p.Id);

                ids.Should().BeEquivalentTo(2);
            }
        }

        [Fact]
        public void BeIgnored()
        {
            using (TestDatabase test = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            using (IDatabase database = CreateDatabase(test))
            {
                var ids = database.Query<Foo>()
                    .IgnoreQueryFilters()
                    .Where(p => p.Id > 1)
                    .ToList()
                    .Select(p => p.Id);

                ids.Should().BeEquivalentTo(2, 3, 4);
            }
        }

        [Fact]
        public void ApplyQueryFilterToFirstOrDefaultCondition()
        {
            using (TestDatabase test = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            using (IDatabase database = CreateDatabase(test))
            {
                Foo foo = database.Query<Foo>()
                    .FirstOrDefault(p => p.Id == 3);

                foo.Should().BeNull();
            }
        }

        [Fact]
        public void ApplyQueryFilterToScalarCondition()
        {
            using (TestDatabase test = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            using (IDatabase database = CreateDatabase(test))
            {
                int sum = database.Query<Foo>()
                    .Sum(p => p.Id);

                sum.Should().Be(3);
            }
        }

        [Fact]
        public void BeIgnoredIfScalarQuery()
        {
            using (TestDatabase test = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            using (IDatabase database = CreateDatabase(test))
            {
                int sum = database.Query<Foo>()
                    .IgnoreQueryFilters()
                    .Sum(p => p.Id);

                sum.Should().Be(10);
            }
        }

        private IDatabase CreateDatabase(TestDatabase database)
            => Database.Builder
                .UseConnection(database.ConnectionString)
                .UseDatabaseConfiguration<DatabaseConfiguration>()
                .Build();

        public class DatabaseConfiguration : DatabaseConfigurationBase
        {
            public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Table("Foo")
                    .UseQueryFilter<Foo>(f => f.UserId == 1);
            }
        }
    }
}
