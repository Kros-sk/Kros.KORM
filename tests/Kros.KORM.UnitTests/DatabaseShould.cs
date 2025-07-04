using FluentAssertions;
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

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void HasActiveConnectionWithDefaultModelBuilder()
        {
            using (var connection = new SqlConnection())
            using (var database = new Database(connection))
            {
                database.ModelBuilder.Should().NotBeNull();
            }
        }

        [Fact]
        public void CreateQuery()
        {
            using (var connection = new SqlConnection())
            using (var database = new Database(connection))
            {
                database.Query<Person>().Should().NotBeNull();
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
                result.Should().Be("true");
            }
        }

        private class Person
        {
        }
    }
}
