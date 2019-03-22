using FluentAssertions;
using Kros.KORM.UnitTests.Base;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class LinqProviderShould : DatabaseTestBase
    {
        #region SQL Scripts

        private const string Table_TestTable = "TestTable";

        private static string CreateTable_TestTable =
$@"CREATE TABLE[dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Number] [int] NOT NULL,
    [Description] [nvarchar] (50) NULL
) ON[PRIMARY];

INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (1, 10, 'Lorem ipsum');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (2, 20, NULL);
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (3, 20, 'Hello world');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (4, 40, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (5, 50, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (6, 60, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (7, 70, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (8, 80, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (9, 90, 'Nothing special');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (10, 100, 'Nothing special');
";

        #endregion

        [Fact]
        public void ExecuteWhereWithLikeCondition()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Where(p => p.Description.Contains("or"))
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 1, 3 });
            }
        }

        [Fact]
        public void ExecuteFirstOrDefalt()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .FirstOrDefault(p => p.Id == 4);

                actual.Id.Should().Be(4);
            }
        }

        [Fact]
        public void ExecuteTopTwo()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Where(p => p.Id > 0)
                    .Take(2)
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 1, 2 });
            }
        }

        [Fact]
        public void Skip8Rows()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Skip(8)
                    .OrderBy(p => p.Id)
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 9, 10 });
            }
        }

        [Fact]
        public void Skip2RowsAndReturnNext3()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Skip(2)
                    .Take(3)
                    .OrderBy(p => p.Id)
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 3, 4, 5 });
            }
        }

        [Fact]
        public void Skip2RowsAndReturnNext3WithCondition()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Where(p => p.Id > 4)
                    .Skip(2)
                    .Take(3)
                    .OrderBy(p => p.Id)
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 7, 8, 9 });
            }
        }

        [Fact]
        public void ExecuteOrderBy()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .OrderByDescending(p => p.Number)
                    .ThenBy(p => p.Id)
                    .ToList();

                actual.Select(p => p.Id).Should().BeEquivalentTo(new List<int>() { 10, 9, 8, 7, 6, 5, 4, 2, 3, 1 });
            }
        }

        [Fact]
        public void ExecuteCount()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Where(p => p.Id > 2)
                    .Count();

                actual.Should().Be(8);
            }
        }

        [Fact]
        public void ExecuteMin()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Min(p => p.Number);

                actual.Should().Be(10);
            }
        }

        [Fact]
        public void ExecuteMax()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Where(p => p.Number < 30)
                    .Max(p => p.Number);

                actual.Should().Be(20);
            }
        }

        [Fact]
        public void ExecuteSum()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Sum(p => p.Number);

                actual.Should().Be(540);
            }
        }

        [Fact]
        public void ExecuteFirstOrDefaultAfterQueryBuilder()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm
                    .Query<TestTable>()
                    .Select("Id")
                    .Where("Id > @1", 1)
                    .FirstOrDefault();

                actual.Id.Should().Be(2);
            }
        }

        [Fact]
        public void ExecuteAnyWithCondition()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var any = korm
                    .Query<TestTable>()
                    .Any(p => p.Id > 3);

                any.Should().BeTrue();
            }
        }

        [Fact]
        public void ExecuteAnyWithConditionWhichReturnFalse()
        {
            using (var korm = CreateDatabase(CreateTable_TestTable))
            {
                var any = korm
                    .Query<TestTable>()
                    .Any(p => p.Id > 50);

                any.Should().BeFalse();
            }
        }

        private class TestTable
        {
            public int Id { get; set; }

            public int Number { get; set; }

            public string Description { get; set; }
        }
    }
}
