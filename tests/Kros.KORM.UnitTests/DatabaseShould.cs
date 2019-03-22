using FluentAssertions;
using Kros.Data.SqlServer;
using Kros.KORM.UnitTests.Integration;
using Kros.UnitTests;
using System;
using System.Data.SqlClient;
using Xunit;

namespace Kros.KORM.UnitTests
{
    public class DatabaseShould
    {
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

            using (var testHelper = new SqlServerTestHelper(IntegrationTestConfig.ConnectionString, dbName))
            using (IDatabase database = new Database(testHelper.Connection))
            {
                SqlServerIdGeneratorFactory.Register();
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