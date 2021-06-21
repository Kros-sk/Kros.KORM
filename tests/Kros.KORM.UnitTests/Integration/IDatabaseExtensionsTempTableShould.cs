using FluentAssertions;
using Kros.Extensions;
using Kros.KORM.UnitTests.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public partial class IDatabaseExtensionsShould : DatabaseTestBase
    {
        private static readonly string InsertDataScript2 =
            $@"INSERT INTO {Table_TestTable} VALUES (1, 18, 'John', 'Smith');
            INSERT INTO {Table_TestTable} VALUES (2, 22, 'Kilie', 'Bistrol');
            INSERT INTO {Table_TestTable} VALUES (3, 77, 'Adam', 'Pribela');
            INSERT INTO {Table_TestTable} VALUES (4, 66, 'Jardo', 'Hornak');
            INSERT INTO {Table_TestTable} VALUES (5, 2, 'Marian', 'Matula');
            INSERT INTO {Table_TestTable} VALUES (6, 122, 'Michal', 'Matis');
            INSERT INTO {Table_TestTable} VALUES (7, 212, 'Peter', 'Kadasi');
            INSERT INTO {Table_TestTable} VALUES (8, 272, 'Aurel', 'Macak');
            INSERT INTO {Table_TestTable} VALUES (9, 227, 'Zuzka', 'Revakova');
            INSERT INTO {Table_TestTable} VALUES (10, 242, 'Andrej', 'Hlava');
            INSERT INTO {Table_TestTable} VALUES (11, 122, 'Johny', 'Slivka');";

        [Fact]
        public void ExecuteWithTempTableList()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var ids = new List<int>() { 1, 2, 3, 4, 456, 789 };
                var affectedCount = database.ExecuteWithTempTable(
                    ids,
                    (database, tableName) => database.ExecuteNonQuery(
                        $@"UPDATE P
                          SET P.Age = 18
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.Value)"));

                affectedCount.Should().Be(4);
            }
        }

        [Fact]
        public async Task ExecuteWithTempTableListAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var ids = new List<int>() { 1, 2, 3, 4, 456, 789 };
                var affectedCount = await database.ExecuteWithTempTableAsync(
                    ids,
                    (database, tableName) => database.ExecuteNonQueryAsync(
                        $@"UPDATE P
                          SET P.Age = 18
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.Value)"));

                affectedCount.Should().Be(4);
            }
        }

        [Fact]
        public void ExecuteWithTempTableTList()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var ids = new List<int>() { 1, 2, 3, 4, 45 };
                List<Person> result = database.ExecuteWithTempTable(
                    ids,
                    (database, tableName) => database.Query<Person>()
                    .From($"People AS P INNER JOIN {tableName} AS T ON (P.Id = T.Value)")
                    .ToList());
                result.Should().HaveCount(4);
            }
        }

        [Fact]
        public async Task ExecuteWithTempTableTListAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var ids = new List<int>() { 1, 2, 3, 4, 45 };
                IEnumerable<Person> result = await database.ExecuteWithTempTableAsync(
                    ids,
                    (database, tableName) => database.Query<Person>()
                    .From($"People AS P INNER JOIN {tableName} AS T ON (P.Id = T.Value)")
                    .ToList()
                    .AsTask());

                result.Should().HaveCount(4);
            }
        }

        [Fact]
        public void ExecuteWithTempTableDictionary()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var names = new Dictionary<int, string>() { { 1, "jedna" }, { 2, "dva" }, { 12, "dvanast" } };

                var affectedCount = database.ExecuteWithTempTable(
                    names,
                    (database, tableName) => database.ExecuteNonQuery(
                        @$"UPDATE P
                          SET P.FirstName = T.Value
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])"));

                affectedCount.Should().Be(2);
            }
        }

        [Fact]
        public async Task ExecuteWithTempTableDictionaryAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var names = new Dictionary<int, string>() { { 1, "jedna" }, { 2, "dva" }, { 12, "dvanast" } };

                var affectedCount = await database.ExecuteWithTempTableAsync(
                    names,
                    (database, tableName) => database.ExecuteNonQueryAsync(
                        @$"UPDATE P
                          SET P.FirstName = T.Value
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])"));

                affectedCount.Should().Be(2);
            }
        }

        [Fact]
        public void ExecuteWithTempTableTDictionary()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var names = new Dictionary<int, string>() { { 1, "jedna" }, { 2, "dva" }, { 12, "dvanast" } };

                List<Person> result = database.ExecuteWithTempTable(
                    names,
                    (database, tableName) =>
                    {
                        database.ExecuteNonQuery(
                            @$"UPDATE P
                          SET P.FirstName = T.Value
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])");

                        return database.Query<Person>()
                            .From($"People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])")
                            .ToList();
                    });

                result.Should().HaveCount(2);
            }
        }

        [Fact]
        public async Task ExecuteWithTempTableTDictionaryAsync()
        {
            using (IDatabase database = CreateDatabase(CreateTable_TestTable, InsertDataScript2))
            {
                var names = new Dictionary<int, string>() { { 1, "jedna" }, { 2, "dva" }, { 12, "dvanast" } };

                IEnumerable<Person> result = await database.ExecuteWithTempTableAsync(
                    names,
                    async (database, tableName) =>
                    {
                        await database.ExecuteNonQueryAsync(
                            @$"UPDATE P
                          SET P.FirstName = T.Value
                          FROM People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])");

                        return await database.Query<Person>()
                            .From($"People AS P INNER JOIN {tableName} AS T ON (P.Id = T.[Key])")
                            .ToList()
                            .AsTask();
                    });

                result.Should().HaveCount(2);
            }
        }
    }
}
