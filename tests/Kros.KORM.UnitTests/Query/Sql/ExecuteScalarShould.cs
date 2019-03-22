using FluentAssertions;
using Kros.KORM.UnitTests.Base;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    public class ExecuteScalarShould: DatabaseTestBase
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
";

        #endregion

        [Fact]
        public void ReturnIntValue()
        {
            using(var korm = CreateDatabase(CreateTable_TestTable))
            {
                var query = $"SELECT [Id] FROM {Table_TestTable} WHERE [Description] = @description";
                var actual = korm.ExecuteScalar<int>(query, "Hello world");

                actual.Value.Should().Be(3);
            }
        }

        [Fact]
        public void ReturnStringValue()
        {
            using(var korm = CreateDatabase(CreateTable_TestTable))
            {
                var query = $"SELECT [Description] FROM {Table_TestTable} WHERE [Id] = @id";
                var actual = korm.ExecuteScalar(query, 3);

                actual.Should().Be("Hello world");
            }
        }

        [Fact]
        public void ReturnNullWhenRecordNotExists()
        {
            using(var korm = CreateDatabase(CreateTable_TestTable))
            {
                var actual = korm.ExecuteScalar<int>($"SELECT [Number] FROM {Table_TestTable} WHERE [Id] = @id", -1);

                actual.Should().NotHaveValue();
            }
        }
    }
}