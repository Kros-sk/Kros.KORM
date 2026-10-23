using Kros.Data.SqlServer;
using Kros.UnitTests;
using Microsoft.Data.SqlClient;
using System;
using Xunit;

namespace Kros.KORM.UnitTests
{
    [Collection(KormTestsCollection.Name)]
    public class DatabaseShould
    {
        private readonly KormTestsFixture _kormContext;

        public DatabaseShould(KormTestsFixture kormContext)
        {
            _kormContext = kormContext;
        }

        [Fact]
        public void ThrowExceptionWhenActiveConnectionIsNull()
        {
            SqlConnection connection = null;
            Action action = () =>
            {
                IDatabase database = new Database(connection);
            };

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void HasActiveConnectionWithDefaultModelBuilder()
        {
            using (var connection = new SqlConnection())
            using (var database = new Database(connection))
            {
                Assert.NotNull(database.ModelBuilder);
            }
        }

        [Fact]
        public void CreateQuery()
        {
            using (var connection = new SqlConnection())
            using (var database = new Database(connection))
            {
                Assert.NotNull(database.Query<Person>());
            }
        }

        [Fact]
        public void InitForIdGenerator()
        {
            string dbName = $"KORM_InitIdGenerator";
            string idStoreTableName = "IdStore";

            using (var testHelper = new SqlServerTestHelper(_kormContext.GetConnectionString(), dbName) { DropDatabaseOnDispose = false })
            using (IDatabase database = new Database(testHelper.Connection))
            {
                SqlServerIntIdGeneratorFactory.Register();
                database.InitDatabaseForIdGenerator();

                var result = database.ExecuteScalar(
                    $"IF EXISTS (SELECT 1 FROM sys.Tables WHERE Name = N'{idStoreTableName}' AND Type = N'U') " +
                     "SELECT 'true' ELSE SELECT 'false'");
                Assert.Equal("true", result);
            }
        }

        private class Person
        {
        }
    }
}
